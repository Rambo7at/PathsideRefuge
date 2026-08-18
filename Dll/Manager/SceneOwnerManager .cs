using Godot;
using System.Collections.Generic;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using static Godot.MultiplayerPeer;

/// <summary>注：场景拥有者管理器，全局单例，负责管理各场景所有权归属</summary>
public partial class SceneOwnerManager : Node
{
    private static SceneOwnerManager _instance;
    public static SceneOwnerManager Instance => _instance ??= new();

    /// <summary>注：场景哈希 → 当前拥有者 PeerID</summary>
    private Dictionary<int, long> _sceneOwners = [];

    /// <summary>注：玩家 PeerID → 所在场景哈希</summary>
    private Dictionary<long, int> _playerSceneDict = [];



    public override void _Ready()
    {
        _instance = this;
        CatLog.Ok("[SceneOwnerManager] 初始化完成");
    }

    /// <summary>注：请求获取场景拥有权（任意端调用，服务器处理）</summary>
    public void RequestSceneOwnership(int sceneHash, long sendPeer) => RpcId(NetCore.ServerID, nameof(Rpc_RequestSceneOwnership), sceneHash, sendPeer);

    /// <summary>注：转移场景拥有权（由离开场景的拥有者调用）</summary>
    public void TransferOwnership(int sceneHash, long requestingPeer) => RpcId(NetCore.ServerID, nameof(Rpc_TransferOwnership), sceneHash, requestingPeer);

    /// <summary>注：广播询问场景中是否还有存活的对等端</summary>
    private void BroadcastQuerySceneActive(int sceneHash, long requestingPeer)
    {
        var peers = Multiplayer.GetPeers();

        foreach (var peer in peers)
        {
            if (peer == requestingPeer) continue;
            RpcId(peer, nameof(Rpc_QuerySceneActive), sceneHash);
        }
    }

    /// <summary>注：清空所有场景拥有权（用于主机启动新游戏会话时重置）</summary>
    public void ClearAll()
    {
        _sceneOwners.Clear();
        CatLog.Debug("[SceneOwnerManager] 所有场景拥有权已清空");
    }

    public void HandlePlayerDisconnected(long peer)
    {
        if (NetCore.Instance.IsClient) return;

        if (!_playerSceneDict.TryGetValue(peer, out var sceneHash)) return;

        _sceneOwners.TryGetValue(sceneHash, out var ownerPeer);

        if (ownerPeer == peer)
        {
            _sceneOwners.Remove(sceneHash);
            BroadcastQuerySceneActive(sceneHash, ownerPeer);
        }

        _playerSceneDict.Remove(peer);
    }


    public bool IsPlayerInScene(long peer, int sceneHash)
    {
        if (!_playerSceneDict.TryGetValue(peer, out int hash)) return false;

        return sceneHash == hash;
    }


    public bool IsPlayerConnected(long id) => !NetCore.Instance.HasPeer((int)id);



    /// <summary>注：服务器接收拥有权请求，无主则分配，有主则通知或夺取</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = TransferModeEnum.Reliable, CallLocal = true)]
    private void Rpc_RequestSceneOwnership(int sceneHash, long sendPeer)
    {
        if (NetCore.Instance.IsClient) return;

        _playerSceneDict[sendPeer] = sceneHash;   // 这里是玩家进入场景的铁证，所有进入场景都要经过这里

        if (!_sceneOwners.TryGetValue(sceneHash, out long peer))
        {
            _sceneOwners[sceneHash] = sendPeer;
            RpcId(sendPeer, nameof(Rpc_GrantSceneOwnership), sceneHash, sendPeer);
            CatLog.Ok($"[SceneOwnerManager]此场景无拥有者，[{sendPeer}] 已成为拥有者");
            return;
        }

        long ownerPeer = peer;

        if (sendPeer == NetCore.ServerID)
        {
            ownerPeer = NetCore.ServerID;
            _sceneOwners[sceneHash] = ownerPeer;

            RpcId(sendPeer, nameof(Rpc_GrantSceneOwnership), sceneHash, sendPeer);
            BroadcastQuerySceneActive(sceneHash, sendPeer);
            return;
        }

        RpcId(sendPeer, nameof(Rpc_GrantSceneOwnership), sceneHash, ownerPeer);
    }

    /// <summary>注：授予场景拥有权，触发场景的数据恢复或请求逻辑</summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = TransferModeEnum.Reliable, CallLocal = true)]
    public void Rpc_GrantSceneOwnership(int sceneHash, long ownerPeer)
    {
        if (sceneHash != WorldManager.Instance.CurrentSceneHash) return;
        WorldManager.Instance.CurrentScene.OnOwnershipGranted(ownerPeer);
    }

    /// <summary>注：服务器处理拥有权转移，移除旧拥有者并询问存活者</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = TransferModeEnum.Reliable, CallLocal = true)]
    public void Rpc_TransferOwnership(int sceneHash, long requestingPeer)
    {
        if (NetCore.Instance.IsClient) return;
        if (!_sceneOwners.TryGetValue(sceneHash, out _)) return;

        _sceneOwners.Remove(sceneHash);
        BroadcastQuerySceneActive(sceneHash, requestingPeer);
    }

    /// <summary>注：询问场景中是否还有存活者，仍存活则重新请求拥有权</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = TransferModeEnum.Reliable, CallLocal = true)]
    public void Rpc_QuerySceneActive(int sceneHash)
    {
        if (sceneHash != WorldManager.Instance.CurrentSceneHash) return;
        RequestSceneOwnership(sceneHash, NetCore.Instance.LocalPeerID);
    }


}