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

    private readonly System.Collections.Generic.Dictionary<NetID, NetObject> _netObjects = [];

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

        NetObject netobj = new(id, hash, pos, rot);

        _netObjects[id] = netobj;

        if (NetCore.Instance.IsHost)
        {
            Rpc(nameof(Rpc_HostSyncRegister), id.OwnerPeerID, id.LocalSeqId, id.SceneHash, netobj.PrefabHash, pos, rot);
            return id;
        }
        else
        {
            Rpc(nameof(Rpc_ReportToServer), id.OwnerPeerID, id.LocalSeqId, id.SceneHash, netobj.PrefabHash, pos, rot);
            return id;
        }
    }

    public NetID RegisterObject(NetObject netobj, Vector3 pos, Vector3 rot)
    {
        NetID id = GetNetID();
        netobj.netId = id;

        _netObjects[id] = netobj;

        if (NetCore.Instance.IsHost)
        {
            Rpc(nameof(Rpc_HostSyncRegister), id.OwnerPeerID, id.LocalSeqId, id.SceneHash, netobj.PrefabHash, pos, rot);
            return id;
        }
        else
        {
            Rpc(nameof(Rpc_ReportToServer), id.OwnerPeerID, id.LocalSeqId, id.SceneHash, netobj.PrefabHash, pos, rot);
            return id;
        }
    }

    public void RegisterObjectLocal(Array<NetObject> netObjects)
    {
        foreach (var item in netObjects)
        {
            if (_netObjects.ContainsKey(item.netId)) continue;
            _netObjects[item.netId] = item;
            OnSpawned?.Invoke(item.netId, null);
        }
    }

    /// <summary>注：主机同步注册网络对象信息，并触发对象生成事件。</summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    private void Rpc_HostSyncRegister(long ownerPeer, uint seqId, int sceneHash,int prefabHash, Vector3 pos, Vector3 rot )
    {
        NetID netId = new(ownerPeer, seqId, sceneHash);

        if (_netObjects.ContainsKey(netId)) return;

        var netobj = new NetObject(netId, prefabHash,pos, rot);

        _netObjects[netId] = netobj;

        OnSpawned?.Invoke(netId, null);
    }

    /// <summary>注：向服务器报告网络对象信息，服务器登记并广播给其他客户端，触发对象生成事件。</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void Rpc_ReportToServer(long ownerPeer, uint seqId, int sceneHash, int prefabHash, Vector3 pos, Vector3 rot)
    {
        if (NetCore.Instance.IsClient) return;

        NetID netId = new(ownerPeer, seqId, sceneHash);

        if (_netObjects.ContainsKey(netId)) return;

        var netobj = new NetObject(netId, prefabHash, pos, rot);
        _netObjects[netId] = netobj;

        long senderId = Multiplayer.GetRemoteSenderId();
        foreach (long peerId in Multiplayer.GetPeers())
        {
            if (peerId != senderId && peerId != NetCore.ServerID)
            {
                RpcId(peerId, nameof(Rpc_HostSyncRegister), netId.OwnerPeerID, netId.LocalSeqId, netId.SceneHash, netobj.PrefabHash, pos, rot);
            }
        }

        OnSpawned?.Invoke(netId, null);
    }

    /// <summary>注：通知所有客户端销毁指定网络对象</summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    public void Rpc_DestroyNetObject(long ownerPeer, uint seqId, int sceneHash)
    {
        if (NetCore.Instance.IsHost) return;

        NetID id = new(ownerPeer, seqId, sceneHash);

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

        NetID id = netObject.netId;
        Rpc(nameof(Rpc_DestroyNetObject), id.OwnerPeerID, id.LocalSeqId, id.SceneHash);
        CatLog.Net($"[NetObjectRegistry] 主机广播销毁对象：{id}");
    }

    public void RemoveNet(NetID ID)
    {
        if (!_netObjects.TryGetValue(ID, out var netObj)) return;
        _netObjects.Remove(ID);
        OnDestroyed?.Invoke(ID);
    }

    /// <summary>注：根据网络对象 ID 获取网络对象。</summary>
    public NetObject GetNetObject(NetID id) => _netObjects.TryGetValue(id, out var netobj) ? netobj : null;

    /// <summary>注：获取指定场景的所有网络对象身份信息列表</summary>
    public Array<NetObject> GetNetObjectsForScene(int sceneHash)
    {
        Array<NetObject> arr = [];

        foreach (var netobj in _netObjects)
        {
            if (netobj.Key.SceneHash != sceneHash) continue;
            arr.Add(netobj.Value);
        }

        return arr;
    }

    public void GetAllNetObjects()
    {
        foreach (var netObj in _netObjects)
        {
            CatLog.Debug($"[NetObjectRegistry] NetID: {netObj.Key}, PrefabHash: {netObj.Value.PrefabHash}，ObjNetID：{netObj.Value.netId}");
        }
    }
}
