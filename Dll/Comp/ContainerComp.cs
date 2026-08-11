using Godot;
using 维修公司.Dll.data;
using 维修公司.Dll.Interface;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using 途畔归所.Dll.View;


namespace 途畔归所.Dll.Comp;

/// <summary>注：容器交互组件，挂载于箱子/容器类放置物上，处理打开/关闭、库存同步与网络通信</summary>
public partial class ContainerComp : PlacedBase, IInteractable, IInventoryHolder
{
    [Export] private InventoryData m_inventoryData;
    [Export] private Node3D m_dropPos;

    private InventoryView _inventoryView;
    private NetSyncBase _netSyncBase;

    /// <summary>注：容器是否处于打开状态</summary>
    public bool m_IsOpen { get; private set; }

    InventoryData IInventoryHolder.InventoryData { get => m_inventoryData; set => m_inventoryData = value; }
    Vector3 IInventoryHolder.DropPos => m_dropPos.GlobalPosition;

    public string ObjectName => m_placedData.m_Name;

    public override void _Ready()
    {
        if (CatUtils.FindChildNode<NetSyncBase>(this) is not NetSyncBase sync)
        {
            CatUtils.StopAndExit(this);
            return;
        }

        if (m_inventoryData == null)
        {
            CatUtils.StopAndExit(this);
            CatLog.Err("[ContainerComp._Ready]：ContainerComp 缺少 m_inventoryData 数据 ，已销毁");
            return;
        }

        _netSyncBase = sync;

        if (InitSync(_netSyncBase) == false)
        {
            CatUtils.StopAndExit(this);
            return;
        }

        if (GUIManager.Instance.GetView(m_inventoryData.m_UIname) is not InventoryView view)
        {
            CatLog.Err("[ContainerComp._Ready] 箱子视图加载失败");
            CatUtils.StopAndExit(this);
            return;
        }
        _inventoryView = view;
        _inventoryView.m_holder = this;
        _inventoryView.Visible = false;

        // ─── RPC 注册 ──────────────────────────────────────────────
        // 注：状态同步使用 int（0/1）而非 bool，规避 Godot 序列化问题
        _netSyncBase.RegisterRpc("RPC_RequestOpenContainer", RPC_RequestOpenContainer);
        _netSyncBase.RegisterRpc<byte[]>("RPC_ReceiveContainerInventory", RPC_ReceiveContainerInventory);
        _netSyncBase.RegisterRpc<int>("RPC_SyncContainerOpenState", RPC_SyncContainerOpenState); // ✅ int 参数
        _netSyncBase.RegisterRpc("RPC_RequestCloseContainer", RPC_RequestCloseContainer);
        _netSyncBase.RegisterRpc("RPC_ReceiveCloseContainer", RPC_ReceiveCloseContainer);
        _netSyncBase.RegisterRpc<byte[]>("RPC_SubmitFinalInventory", RPC_SubmitFinalInventory);
    }

    private bool InitSync(NetSyncBase netSync)
    {
        if (netSync == null || netSync.NetObj == null)
        {
            CatLog.Warn("[ContainerComp.InitContainerNetSync] NetSyncBase 或 NetSyncBase.NetObj 为空");
            return false;
        }
        NetObject netObject = netSync.NetObj;

        netSync.OnSaveState += () => FlushInventory(netSync.NetObj);

        var custdata = netObject.m_customData.As<PlacedData>();
        m_placedData = custdata != null ? custdata.DeepCopy() : m_placedData;

        var data = m_placedData.m_data.As<InventoryData>();
        m_inventoryData = (data?.m_itemArr == null || data.m_itemArr.Count == 0) ? m_inventoryData : data;

        return true;
    }

    public override void _Process(double delta)
    {
        if (m_IsOpen && _inventoryView.GetParent() == PlayerManager.Instance.LocalPlayer.GUI)
        {
            float distance = GlobalPosition.DistanceTo(PlayerManager.Instance.LocalPlayer.GlobalPosition);
            if (distance >= 3f)
            {
                CloseContainer();
            }
        }
    }

    public void OpenContainer(Player player)
    {
        if (player == null || _inventoryView == null) return;
        if (m_IsOpen) return;

        if (NetCore.Instance.IsClient)
        {
            CatLog.Ok("[ContainerComp] 客户端请求打开容器");
            _netSyncBase.SendRpcToHost("RPC_RequestOpenContainer");
            return;
        }

        CatLog.Ok($"[ContainerComp] 主机打开容器，m_IsOpen 当前值：{m_IsOpen}");
        PlayerManager.Instance.LocalPlayer.GUI.AddChild(_inventoryView);
        _inventoryView.Visible = true;
        m_IsOpen = true;

        // ✅ 发送 int（1 = true）
        _netSyncBase.SendRpcBroadcast("RPC_SyncContainerOpenState", m_IsOpen ? 1 : 0);
        CatLog.Ok($"[ContainerComp] 主机打开完成，m_IsOpen 设为 true");
    }

    public void CloseContainer()
    {
        if (!m_IsOpen) return;
        bool isUser = _inventoryView.GetParent() == PlayerManager.Instance.LocalPlayer.GUI;
        if (isUser == false) return;

        if (NetCore.Instance.IsClient)
        {
            CatLog.Ok("[ContainerComp] 客户端请求关闭容器");
            _netSyncBase.SendRpcToHost("RPC_RequestCloseContainer");
            return;
        }

        CatLog.Ok($"[ContainerComp] 主机关闭容器，m_IsOpen 当前值：{m_IsOpen}");
        _inventoryView.GetParent()?.RemoveChild(_inventoryView);
        m_IsOpen = false;

        // ✅ 发送 int（0 = false）
        _netSyncBase.SendRpcBroadcast("RPC_SyncContainerOpenState", m_IsOpen ? 1 : 0);
        CatLog.Ok($"[ContainerComp] 主机关闭完成，m_IsOpen 设为 false");
    }

    // ─── RPC 接收方法 ──────────────────────────────────────────────

    private void RPC_RequestOpenContainer()
    {
        if (NetCore.Instance.IsClient) return;
        if (m_IsOpen)
        {
            CatLog.Warn("[ContainerComp] RPC_RequestOpenContainer：容器已打开，忽略");
            return;
        }

        long requesterId = Multiplayer.GetRemoteSenderId();
        CatLog.Ok($"[ContainerComp] 主机收到客户端 {requesterId} 的打开请求");

        byte[] bytes = m_inventoryData.Serialize();
        m_IsOpen = true;

        // ✅ 发送 int（1 = true）
        _netSyncBase.SendRpcBroadcast("RPC_SyncContainerOpenState", m_IsOpen ? 1 : 0);
        _netSyncBase.SendRpcToPeer("RPC_ReceiveContainerInventory", requesterId, bytes);

        CatLog.Ok($"[ContainerComp] 主机已打开容器，m_IsOpen 设为 true，已发送库存数据给 {requesterId}");
    }

    private void RPC_RequestCloseContainer()
    {
        if (NetCore.Instance.IsClient) return;
        if (!m_IsOpen)
        {
            CatLog.Warn("[ContainerComp] RPC_RequestCloseContainer：容器已关闭，忽略");
            return;
        }

        long requesterId = Multiplayer.GetRemoteSenderId();
        CatLog.Ok($"[ContainerComp] 主机收到客户端 {requesterId} 的关闭请求");

        m_IsOpen = false;

        // ✅ 发送 int（0 = false）
        _netSyncBase.SendRpcBroadcast("RPC_SyncContainerOpenState", m_IsOpen ? 1 : 0);
        _netSyncBase.SendRpcToPeer("RPC_ReceiveCloseContainer", requesterId);

        CatLog.Ok($"[ContainerComp] 主机已关闭容器，m_IsOpen 设为 false，已通知 {requesterId}");
    }

    /// <summary>注：客户端接收主机关闭通知 → 关闭视图 + 提交最终库存</summary>
    private void RPC_ReceiveCloseContainer(long senderId)
    {
        if (senderId != NetCore.ServerID || NetCore.Instance.IsHost) return;

        CatLog.Ok($"[ContainerComp] 客户端收到主机关闭通知");
        _inventoryView.GetParent()?.RemoveChild(_inventoryView);
        m_IsOpen = false;

        byte[] finalInventoryData = m_inventoryData.Serialize();
        _netSyncBase.SendRpcToHost("RPC_SubmitFinalInventory", finalInventoryData);

        CatLog.Ok("[ContainerComp] 客户端已关闭视图，已提交最终库存数据");
    }

    /// <summary>注：主机接收客户端提交的最终库存数据并保存</summary>
    private void RPC_SubmitFinalInventory(long requesterId, byte[] data)
    {
        if (NetCore.Instance.IsClient) return;

        if (m_IsOpen)
        {
            CatLog.Warn($"[ContainerComp] 客户端 {requesterId} 在容器未关闭时提交数据，已拒绝");
            return;
        }
        if (data == null || data.Length == 0)
        {
            CatLog.Warn($"[ContainerComp] 客户端 {requesterId} 提交了空的库存数据");
            return;
        }

        InventoryData finalData = new InventoryData();
        finalData.Deserialize(data);
        m_inventoryData = finalData.DeepCopy();

        if (_inventoryView.GetParent() == PlayerManager.Instance.LocalPlayer.GUI)
            _inventoryView.RefreshAllSlots();

        CatLog.Ok($"[ContainerComp] 客户端 {requesterId} 的最终库存数据已保存");
    }

    /// <summary>注：客户端接收主机同步的容器库存数据 → 显示视图</summary>
    private void RPC_ReceiveContainerInventory(long senderId, byte[] data)
    {
        if (NetCore.Instance.IsHost) return;

        if (data == null)
        {
            CatLog.Warn("[ContainerComp] RPC_ReceiveInventoryData：数据包为空");
            return;
        }

        CatLog.Ok("[ContainerComp] 客户端收到库存数据，显示视图");
        InventoryData inventoryData = new InventoryData();
        inventoryData.Deserialize(data);
        m_inventoryData = inventoryData.DeepCopy();

        PlayerManager.Instance.LocalPlayer.GUI.AddChild(_inventoryView);
        _inventoryView.Visible = true;
        _inventoryView.RefreshAllSlots();

        CatLog.Ok("[ContainerComp] 客户端视图已显示");
    }

    /// <summary>注：客户端接收主机广播的开关状态同步（int 参数，0=false，1=true）</summary>
    private void RPC_SyncContainerOpenState(long senderId, int state)
    {
        if (NetCore.Instance.IsHost) return;

        bool b = state == 1;
        CatLog.Debug($"[ContainerComp] 客户端收到状态同步：state = {state}, m_IsOpen = {b}");

        m_IsOpen = b;
        if (!b) _inventoryView.GetParent()?.RemoveChild(_inventoryView);
    }

    // ─── 交互接口实现 ──────────────────────────────────────────────

    public void PlayerInteract(bool InputE, bool InputF, CreatureBase creature)
    {
        if (creature is not Player pl) return;

        if (InputE)
        {
            if (m_IsOpen) CloseContainer();
            else OpenContainer(pl);
        }
    }

    // ─── 存档与数据操作 ──────────────────────────────────────────────

    private void FlushInventory(NetObject netObject)
    {
        base.m_placedData.m_data = m_inventoryData.DeepCopy();
        netObject.m_customData = base.m_placedData.DeepCopy();
    }

    public bool TrySetInventoryItem(int index, ItemData data)
    {
        if (index < 0 || index >= m_inventoryData.m_capacity) return false;
        m_inventoryData.m_itemArr[index] = data;
        return true;
    }
}


