using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.NetWork;

/// <summary>注：轻量 RPC 路由器，接收 RPC 并分发到目标节点</summary>
public partial class RpcGateway : Node
{
    private static RpcGateway _instance;
    public static RpcGateway Instance => _instance ??= new();

    public override void _Ready()
    {
        _instance = this;
        CatLog.Ok("[RpcGateway] 初始化完成");
    }



    /// <summary>注：客户端请求获取场景拥有权（向主机询问）</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void Rpc_RequestSceneOwnership(int sceneHash)
    {
        long senderId = Multiplayer.GetRemoteSenderId();

        




    }

    /// <summary>注：主机分配场景拥有者给指定客户端</summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    public void Rpc_AssignSceneOwnership(int sceneHash, long ownerPeerId)
    {
        // 客户端接收：设置本地场景的拥有者
    }












    /// <summary>注：可靠 RPC 接收入口</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void Rpc_Reliable(long ownerPeer, uint seqId, int sceneHash, string name, Variant variant)
    {
        NetID targetId = new(ownerPeer, seqId, sceneHash);

        var node = NetObjectManager.Instance.GetNetObject(targetId);
        if (node == null)
        {
            return;
        }

        if (CatUtils.FindChildNode<NetSyncBase>(node) is not NetSyncBase sync)
        {
            return;
        }

        sync.DispatchRpc(name, variant);
    }

    /// <summary>注：不可靠 RPC 接收入口（用于变换同步等高频数据）</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void Rpc_Unreliable(long ownerPeer, uint seqId, int sceneHash, string name, Variant variant)
    {
        NetID targetId = new(ownerPeer, seqId, sceneHash);

        var node = NetObjectManager.Instance.GetNetObject(targetId);
        if (node == null)
        {
            return;
        }

        if (CatUtils.FindChildNode<NetSyncBase>(node) is not NetSyncBase sync)
        {
            return;
        }

        sync.DispatchRpc(name, variant);
    }


    /// <summary>注：发送 RPC 给主机（无参数）</summary>
    public void CallRpc(NetID target, string name, bool reliable = true)
    {
        if (reliable)
            RpcId(1, nameof(Rpc_Reliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, default);
        else
            RpcId(1, nameof(Rpc_Unreliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, default);
    }

    /// <summary>注：发送 RPC 给主机（单参数）</summary>
    public void CallRpc(NetID target, string name, Variant value, bool reliable = true)
    {
        if (reliable)
            RpcId(1, nameof(Rpc_Reliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, value);
        else
            RpcId(1, nameof(Rpc_Unreliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, value);
    }

    /// <summary>注：发送 RPC 给指定客户端（单参数）</summary>
    public void CallRpc(NetID target, string name, Variant value, long targetPeerId, bool reliable = true)
    {
        if (reliable)
            RpcId(targetPeerId, nameof(Rpc_Reliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, value);
        else
            RpcId(targetPeerId, nameof(Rpc_Unreliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, value);
    }

    /// <summary>注：发送 RPC 给主机（双参数）</summary>
    public void CallRpc(NetID target, string name, Variant v1, Variant v2, bool reliable = true)
    {
        var args = new Godot.Collections.Array { v1, v2 };
        if (reliable)
            RpcId(1, nameof(Rpc_Reliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, args);
        else
            RpcId(1, nameof(Rpc_Unreliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, args);
    }

    /// <summary>注：广播 RPC 给所有客户端（无参数）</summary>
    public void CallAllRpc(NetID target, string name, bool reliable = true)
    {
        if (reliable)
            Rpc(nameof(Rpc_Reliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, default);
        else
            Rpc(nameof(Rpc_Unreliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, default);
    }

    /// <summary>注：广播 RPC 给所有客户端（单参数）</summary>
    public void CallAllRpc(NetID target, string name, Variant value, bool reliable = true)
    {
        if (reliable)
            Rpc(nameof(Rpc_Reliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, value);
        else
            Rpc(nameof(Rpc_Unreliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, value);
    }

    /// <summary>注：广播 RPC 给所有客户端（双参数）</summary>
    public void CallAllRpc(NetID target, string name, Variant v1, Variant v2, bool reliable = true)
    {
        var args = new Godot.Collections.Array { v1, v2 };
        if (reliable)
            Rpc(nameof(Rpc_Reliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, args);
        else
            Rpc(nameof(Rpc_Unreliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, args);
    }
}