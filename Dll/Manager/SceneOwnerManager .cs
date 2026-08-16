using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using static Godot.MultiplayerPeer;

/// <summary>注：场景拥有者管理器，全局单例，负责管理各场景所有权归属</summary>
public partial class SceneOwnerManager : Node
{
    private static SceneOwnerManager _instance;
    public static SceneOwnerManager Instance => _instance ??= new();

    private Dictionary<int, long> _sceneOwners = [];                              // 场景拥有者字典：SceneHash → OwnerPeerID
    private Dictionary<int, TaskCompletionSource<long>> _pendingRequests = [];    // 待处理的请求队列（.NET Dictionary）

    public override void _Ready()
    {
        _instance = this;
        CatLog.Ok("[SceneOwnerManager] 初始化完成");
    }

    public void RequestSceneOwnership(int sceneHash, long requestingPeer) => RpcId(NetCore.ServerID, nameof(Rpc_RequestSceneOwnership), sceneHash, requestingPeer);


    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = TransferModeEnum.Reliable, CallLocal = true)]
    public void Rpc_RequestSceneOwnership(int sceneHash, long requestingPeer)
    {
        if (NetCore.Instance.IsClient) return;

        long sendPeer = Multiplayer.GetRemoteSenderId();

        if (!_sceneOwners.TryGetValue(sceneHash, out long peer))
        {
            _sceneOwners[sceneHash] = requestingPeer;
            RpcId(sendPeer, nameof(Rpc_GrantSceneOwnership), sceneHash, requestingPeer);
            CatLog.Ok($"[SceneOwnerManager]此场景无拥有者，[{requestingPeer}] 已成为拥有者");
            return;
        }

        long ownerPeer = requestingPeer == NetCore.ServerID ? requestingPeer : peer;

        if (ownerPeer == NetCore.ServerID)
        {
            _sceneOwners[sceneHash] = ownerPeer;
            // 这里先不写接管逻辑
        }

        RpcId(sendPeer, nameof(Rpc_NotifySceneOwnership), sceneHash, ownerPeer);

    }

    /// <summary>注：主机回复拥有者（仅客户端接收）</summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = TransferModeEnum.Reliable, CallLocal = true)]
    public void Rpc_GrantSceneOwnership(int sceneHash, long ownerPeer)
    {
        if (sceneHash != WorldManager.Instance.CurrentSceneHash) return;

        WorldManager.Instance.CurrentScene.OnOwnershipAcquired(ownerPeer); 
    }


    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = TransferModeEnum.Reliable, CallLocal = true)]
    public void Rpc_NotifySceneOwnership(int sceneHash, long ownerPeer)
    {

    }

















    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = TransferModeEnum.Reliable, CallLocal = false)]
    public void Rpc_TakeOwnershipNotification(int sceneHash, long newOwner)
    {
        if (NetCore.Instance.IsHost) return;
        if (sceneHash != WorldManager.Instance.CurrentSceneHash) return;

        if (WorldManager.Instance.CurrentScene is SceneBase scene)
        {
            if (scene.OwnerPeerID == newOwner) return;

            scene.OwnerPeerID = newOwner;
            CatLog.Ok($"[SceneOwnerManager] 客户端 {NetCore.Instance.LocalPeerID} 收到场景 {sceneHash} 的接管通知，新拥有者 {newOwner}");
        }
    }




    /// <summary>注：转移场景拥有权（拥有者离开时调用）</summary>
    public void TransferOwnership(int sceneHash, long requestingPeer)
    {
        if (NetCore.Instance.IsHost)
        {
            _sceneOwners.Remove(sceneHash);
            Rpc(nameof(Rpc_RequestOccupants), sceneHash);
            return;
        }

        RpcId(NetCore.ServerID, nameof(Rpc_NotifyLeave), sceneHash);
    }


    /// <summary>注：清空所有场景拥有权（用于主机启动新游戏会话时重置）</summary>
    public void ClearAll()
    {
        _sceneOwners.Clear();
        CatLog.Debug("[SceneOwnerManager] 所有场景拥有权已清空");
    }






    // ─── 所有权转移 RPC ──────────────────────────────────────────────

    // ─── 所有权转移 RPC ──────────────────────────────────────────────

    /// <summary>注：客户端通知主机自己离开场景（由拥有者调用）</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = TransferModeEnum.Reliable)]
    public void Rpc_NotifyLeave(int sceneHash)
    {
        if (NetCore.Instance.IsClient) return;

        CatLog.Ok($"[SceneOwnerManager] 主机收到场景 {sceneHash} 的离开通知，当前拥有者 {_sceneOwners.GetValueOrDefault(sceneHash, 0)}");
        _sceneOwners.Remove(sceneHash);
        CatLog.Ok($"[SceneOwnerManager] 已移除场景 {sceneHash} 的拥有权，广播询问占据者");
        Rpc(nameof(Rpc_RequestOccupants), sceneHash);
    }

    /// <summary>注：询问场景是否还有人（由主机广播）</summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = TransferModeEnum.Reliable, CallLocal = false)]
    public void Rpc_RequestOccupants(int sceneHash)
    {
        if (NetCore.Instance.IsHost) return;
        if (sceneHash != WorldManager.Instance.CurrentSceneHash) return;

        CatLog.Net($"[SceneOwnerManager] 客户端 {NetCore.Instance.LocalPeerID} 收到场景 {sceneHash} 的占据者询问，回复主机");
        RpcId(NetCore.ServerID, nameof(Rpc_ReplyOccupant), sceneHash);
    }

    /// <summary>注：回复主机自己仍在场景中（客户端回复）</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = TransferModeEnum.Reliable)]
    public void Rpc_ReplyOccupant(int sceneHash)
    {
        if (NetCore.Instance.IsClient) return;
        long sendpeer = Multiplayer.GetRemoteSenderId();

        // 已有拥有者则忽略（RPC 有序，第一个到达的生效）
        if (_sceneOwners.TryGetValue(sceneHash, out long currentOwner))
        {
            CatLog.Debug($"[SceneOwnerManager] 场景 {sceneHash} 已有拥有者 {currentOwner}，忽略客户端 {sendpeer} 的回复");
            return;
        }

        _sceneOwners[sceneHash] = sendpeer;
        Rpc(nameof(Rpc_TakeOwnershipNotification), sceneHash, sendpeer);
        CatLog.Ok($"[SceneOwnerManager] 客户端 {sendpeer} 成为场景 {sceneHash} 的新拥有者");
    }




}