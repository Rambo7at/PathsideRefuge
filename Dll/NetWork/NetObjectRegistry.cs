using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Manager;

/// <summary>注：网络对象注册表，管理网络对象注册、同步及相关事件。</summary>
public partial class NetObjectRegistry : Node
{
    private static NetObjectRegistry _instance;
    public static NetObjectRegistry Instance { get => _instance ??= new(); set => _instance ??= value; }

    private readonly System.Collections.Generic.Dictionary<NetID, NetObject> _netObjects = new();

    private uint _nextObjID = 1;

    public event Action<NetID, Node> OnSpawned;
    public event Action<NetID> OnDestroyed;

    public override void _Ready()
    {
        Instance = this;
        CatLog.Ok("[NetObjectRegistry] 已初始化");
    }

    /// <summary>注：获取一个新的网络对象 ID（包含当前场景哈希）。</summary>
    public NetID GetNetID()
    {
        int sceneHash = WorldManager.Instance.CurrentSceneHash;
        return new NetID(NetCore.Instance.LocalPeerID, _nextObjID++, sceneHash);
    }

    /// <summary>注：注册网络对象，主机同步或报告给服务器，并返回对象 ID。</summary>
    public NetID RegisterObject(int hash, Vector3 pos, Vector3 rot)
    {
        NetID id = GetNetID();

        NetObject netobj = new(id, pos, rot, hash, id.UserID);
        netobj.Id = id;
        netobj.sceneHash = id.SceneHash;

        _netObjects[id] = netobj;
        if (NetCore.Instance.IsHost)
        {
            Rpc(nameof(Rpc_HostSyncRegister), id.UserID, id.ID, hash, pos, rot, id.SceneHash);
            return id;
        }
        else
        {
            Rpc(nameof(Rpc_ReportToServer), id.UserID, id.ID, hash, pos, rot, id.SceneHash);
            return id;
        }
    }

    public NetID RegisterObject(NetObject netobj, Vector3 pos, Vector3 rot)
    {
        NetID id = GetNetID();
        netobj.Id = id;
        netobj.sceneHash = id.SceneHash;
        _netObjects[id] = netobj;

        if (NetCore.Instance.IsHost)
        {
            Rpc(nameof(Rpc_HostSyncRegister), id.UserID, id.ID, netobj.PrefabHash, pos, rot, id.SceneHash);
            return id;
        }
        else
        {
            Rpc(nameof(Rpc_ReportToServer), id.UserID, id.ID, netobj.PrefabHash, pos, rot, id.SceneHash);
            return id;
        }
    }

    /// <summary>注：主机同步注册网络对象信息，并触发对象生成事件。</summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    private void Rpc_HostSyncRegister(long userId, uint objId, int hash, Vector3 pos, Vector3 rot, int sceneHash)
    {
        NetID id = new(userId, objId, sceneHash);

        if (!_netObjects.ContainsKey(id))
        {
            var netobj = new NetObject(id, pos, rot, hash, userId);
            netobj.sceneHash = sceneHash;
            _netObjects[id] = netobj;
        }

        OnSpawned?.Invoke(id, null);
    }

    /// <summary>注：向服务器报告网络对象信息，服务器登记并广播给其他客户端，触发对象生成事件。</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void Rpc_ReportToServer(long userId, uint objId, int hash, Vector3 pos, Vector3 rot, int sceneHash)
    {
        if (NetCore.Instance.IsClient) return;

        NetID id = new(userId, objId, sceneHash);

        if (_netObjects.ContainsKey(id))
        {
            CatLog.Net($"[NetObjectRegistry] 重复上报的 NetID: {id}");
            return;
        }

        var netobj = new NetObject(id, pos, rot, hash, userId);
        netobj.sceneHash = sceneHash;
        _netObjects[id] = netobj;

        long senderId = Multiplayer.GetRemoteSenderId();
        foreach (long peerId in Multiplayer.GetPeers())
        {
            if (peerId != senderId && peerId != NetCore.ServerID)
            {
                RpcId(peerId, nameof(Rpc_HostSyncRegister), id.UserID, id.ID, hash, pos, rot, sceneHash);
            }
        }

        OnSpawned?.Invoke(id, null);
    }

    /// <summary>注：通知所有客户端销毁指定网络对象</summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    public void Rpc_DestroyNetObject(long userId, uint objId, int sceneHash)
    {
        if (NetCore.Instance.IsHost) return;

        NetID id = new(userId, objId, sceneHash);

        if (!_netObjects.ContainsKey(id))
        {
            CatLog.Debug($"[NetObjectRegistry] 对象 {id} 已被销毁或不存在");
            return;
        }

        RemoveNet(id);
    }

    /// <summary>注：主机广播销毁网络对象（由主机调用，广播给所有客户端）</summary>
    public void BroadcastDestroyNetObject(NetObject netObject)
    {
        if (netObject == null) return;
        if (!NetCore.Instance.IsHost) return;

        NetID id = netObject.Id;
        Rpc(nameof(Rpc_DestroyNetObject), id.UserID, id.ID, id.SceneHash);
        CatLog.Net($"[NetObjectRegistry] 主机广播销毁对象：{id}");
    }

    public void RemoveNet(NetID ID)
    {
        if (!_netObjects.TryGetValue(ID, out var netObj)) return;
        _netObjects.Remove(ID);
        OnDestroyed?.Invoke(ID);
    }


    /// <summary>注：客户端请求补发指定场景的网络对象</summary>
    public void RequestSceneData(int sceneHash)
    {
        RpcId(1, nameof(RPC_RequestSceneData), sceneHash);
    }

    /// <summary>注：主机接收客户端场景补发请求，返回该场景所有网络对象</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void RPC_RequestSceneData(int sceneHash)
    {
        if (NetCore.Instance.IsClient) return;

        long senderId = Multiplayer.GetRemoteSenderId();

        if (sceneHash != WorldManager.Instance.CurrentSceneHash) return;

        var netObjects = GetNetObjectsForScene(sceneHash);

        if (netObjects == null || netObjects.Count == 0)
        {
            CatLog.Ok($"[NetObjectRegistry] 场景 {sceneHash} 无对象需要补发");
            return;
        }

        CatLog.Ok($"[NetObjectRegistry] 向客户端 {senderId} 补发场景 {sceneHash}，共 {netObjects.Count} 个对象");

        foreach (var netObj in netObjects)
        {
            RpcId(senderId, nameof(Rpc_HostSyncRegister),
                netObj.Id.UserID,
                netObj.Id.ID,
                netObj.PrefabHash,
                netObj.Position,
                netObj.Rotation,
                netObj.Id.SceneHash
            );
        }
    }

    /// <summary>注：根据网络对象 ID 获取网络对象。</summary>
    public NetObject GetNetObject(NetID id) => _netObjects.TryGetValue(id, out var netobj) ? netobj : null;

    /// <summary>注：获取指定场景的所有网络对象身份信息列表</summary>
    public Array<NetObject> GetNetObjectsForScene(int sceneHash)
    {
        Array<NetObject> arr = [];

        foreach (var netobj in _netObjects)
        {
            if (netobj.Value.sceneHash != sceneHash) continue;
            arr.Add(netobj.Value);
        }

        return arr;
    }



    public void GetAllNetObjects()
    {
        foreach (var netObj in _netObjects)
        {
            CatLog.Debug($"[NetObjectRegistry] NetID: {netObj.Key}, PrefabHash: {netObj.Value.PrefabHash}, sceneHash: {netObj.Value.sceneHash}, Pos: {netObj.Value.Position}");
        }
    }
}
