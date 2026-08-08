using Godot;
using System;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.NetWork;

[GlobalClass]
/// <summary>注：网络同步组件，管理RPC注册与调用分发、对象身份注册</summary>
public partial class NetSyncBase : Node
{
    private Node3D _node3D;                   // 挂载的父节点（3D对象）
    private int _nodeHash;                   // 父节点名称哈希值

    public NetObject NetObj { get; set; }    // 网络对象数据
    public bool IsOwner => NetObj != null && NetObj.OwnerPeerID == NetCore.Instance.LocalPeerID;  // 是否为本机所属对象
    public bool IsInit = false;              // 是否已完成初始化

    public System.Collections.Generic.Dictionary<string, Action<long, Variant>> RpcDict { get; set; } = [];  // RPC名称 → 处理委托映射表
    public event Action OnSaveState;     // 场景刷新网络状态时触发

    public override void _EnterTree()
    {
        var node = GetParent();
        if (node is not Node3D node3D)
        {
            CatLog.Err("[NetSyncBase._EnterTree]：挂载的组件对象，不是Node3D类型，已删除");
            node.QueueFree();
            return;
        }

        var scene = WorldManager.Instance.GetCurrentScene();
        RegisterManual(scene, node3D);
    }

    public override void _ExitTree()
    {
        if (NetObj == null) return;

        NetObjectRegistry.Instance.RemoveNet(NetObj.Id);
    }

    /// <summary>注：为场景中手动放置的预制件补注册NetObj身份，防止被系统清理</summary>
    private void RegisterManual(SceneBase sceneBase, Node3D node3D)
    {
        _node3D = node3D;
        _nodeHash = CatUtils.GetStableHashCode(node3D.Name);

        if (NetObj == null)
        {
            if (sceneBase.SceneData.IsNewScene == false)
            {
                CatUtils.StopAndExit(node3D);
                return;
            }
            if (NetCore.Instance.IsHost)
            {
                var ID = NetObjectRegistry.Instance.RegisterObject(_nodeHash, _node3D.GlobalPosition, _node3D.GlobalRotation);
                var netobj = NetObjectRegistry.Instance.GetNetObject(ID);
                NetObj = netobj;
                CatLog.Ok("[NetSyncBase._Ready]：发现未注册组件，已提交注册");
            }
            else
            {
                CatLog.Net("[NetSyncBase._Ready]：对象是客户端，已销毁");
                node3D.QueueFree();
                return;
            }

            NetObj.sceneHash = NetObj.sceneHash == sceneBase.SceneData.SceneHash ? NetObj.sceneHash : sceneBase.SceneData.SceneHash;
        }

        sceneBase.OnSaveState += () => OnSaveState?.Invoke();
        IsInit = true;
    }







    #region RPC 注册
    /// <summary>注：注册无参数RPC</summary>
    public void RegisterRpc(string name, Action action) => RpcDict[name] = (id, _) => action();

    /// <summary>注：注册带发送者ID的RPC</summary>
    public void RegisterRpc(string name, Action<long> action) => RpcDict[name] = (id, _) => action(id);

    /// <summary>注：注册带发送者ID+单参数的RPC</summary>
    public void RegisterRpc<[MustBeVariant] T>(string name, Action<long, T> action) => RpcDict[name] = (id, value) => action(id, value.As<T>());

    /// <summary>注：注册带单参数的RPC</summary>
    public void RegisterRpc<[MustBeVariant] T>(string name, Action<T> action) => RpcDict[name] = (id, value) => action(value.As<T>());

    /// <summary>注：注册带发送者ID+双参数的RPC</summary>
    public void RegisterRpc<[MustBeVariant] T1, [MustBeVariant] T2>(string name, Action<long, T1, T2> action)
    {
        RpcDict[name] = (id, value) =>
        {
            var arr = value.As<Godot.Collections.Array>();

            if (arr == null || arr.Count < 2) return;

            action(id, arr[0].As<T1>(), arr[1].As<T2>());
        };
    }

    /// <summary>注：发送RPC给主机（无参数）</summary>
    public void CallRpc(string name) => RpcId(1, nameof(Rpc_Anypeer), name, default);

    /// <summary>注：发送RPC给主机（单参数）</summary>
    public void CallRpc(string name, Variant value, long Id = 1) => RpcId(Id, nameof(Rpc_Anypeer), name, value);

    /// <summary>注：发送RPC给主机（双参数）</summary>
    public void CallRpc(string name, Variant value1, Variant value2, long Id = 1) => CallRpc(name, new Godot.Collections.Array() { value1, value2 }, Id);

    /// <summary>注：广播RPC给所有客户端（无参数）</summary>
    public void CallAllRpc(string name) => Rpc(nameof(Rpc_Anypeer), name, default);

    /// <summary>注：广播RPC给所有客户端（单参数）</summary>
    public void CallAllRpc(string name, Variant value) => Rpc(nameof(Rpc_Anypeer), name, value);

    /// <summary>注：广播RPC给所有客户端（双参数）</summary>
    public void CallAllRpc(string name, Variant value1, Variant value2) => CallAllRpc(name, new Godot.Collections.Array { value1, value2 });

    /// <summary>注：RPC统一接收入口，所有RPC调用汇聚至此，按name分发至RpcDict对应委托</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void Rpc_Anypeer(string name, Variant variant)
    {
        long senderId = Multiplayer.GetRemoteSenderId();
        if (RpcDict.TryGetValue(name, out var action)) action?.Invoke(senderId, variant);
    }

    #endregion
}