using Godot;
using System;
using System.Collections.Generic;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Manager
{
    /// <summary>注：网络对象注册表，管理网络对象注册、同步及相关事件。</summary>
    public partial class NetObjectRegistry : Node
    {
       
        private static NetObjectRegistry _instance;
        public static NetObjectRegistry Instance { get => _instance ??= new(); set => _instance ??= value; }

        private readonly Dictionary<NetID, NetObject> _netObjects = new();
        private uint _nextObjID = 1;

        public event Action<NetID, Node> OnSpawned;
        public event Action<NetID> OnDestroyed;

        public override void _Ready()
        {
            Instance = this;
            Multiplayer.PeerConnected += OnPeerConnected;
        }

        /// <summary>注：获取一个新的网络对象 ID。</summary>
        public NetID GetNetID() => new(NetCore.Instance.LocalPeerID, _nextObjID++);

        /// <summary>注：注册网络对象，主机同步或报告给服务器，并返回对象 ID。</summary>
        public NetID RegisterObject(int hash, Vector3 pos, Vector3 rot)
        {
            NetID id = GetNetID();
            NetObject netobj = new(id, pos, rot, hash, id.UserID);

            _netObjects[id] = netobj;
            if (NetCore.Instance.IsHost)
            {
                Rpc(nameof(Rpc_HostSyncRegister), id.UserID, id.ID, hash, pos, rot);
                return id;
            }
            else
            {
                Rpc(nameof(Rpc_ReportToServer), id.UserID, id.ID, hash, pos, rot);
                return id;
            }

        }

        /// <summary>注：主机同步注册网络对象信息，并触发对象生成事件。</summary>
        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
        private void Rpc_HostSyncRegister(long userId, uint objId, int hash, Vector3 pos, Vector3 rot)
        {
            NetID id = new(userId, objId);

            if (!_netObjects.ContainsKey(id))
            {
                var netobj = new NetObject(id, pos, rot, hash, userId);
                _netObjects[id] = netobj;
            }

            OnSpawned?.Invoke(id, null);


            //GD.PrintErr($"[NetObjectRegistry]：[目标对象是：{userId}]---[哈希值：{hash}]---[_objects字典内已存在跳过]");
        }

        /// <summary>注：向服务器报告网络对象信息，服务器登记并广播给其他客户端，触发对象生成事件。</summary>
        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
        private void Rpc_ReportToServer(long userId, uint objId, int hash, Vector3 pos, Vector3 rot)
        {
            // 只有服务器执行
            if (!NetCore.Instance.IsHost) return;

            NetID id = new(userId, objId);

            if (_netObjects.ContainsKey(id))
            {
                CatLog.Warn($"[NetObjectRegistry] 重复上报的 NetID: {id}");
                return;
            }

            // 服务器权威登记
            var netobj = new NetObject(id, pos, rot, hash, userId);
            _netObjects[id] = netobj;


            long senderId = Multiplayer.GetRemoteSenderId();
            foreach (long peerId in Multiplayer.GetPeers())
            {
                if (peerId != senderId && peerId != NetCore.ServerID)
                {
                    RpcId(peerId, nameof(Rpc_HostSyncRegister), id.UserID, id.ID, hash, pos, rot);
                }
            }

            OnSpawned?.Invoke(id, null);
        }

        /// <summary>注：新客户端连接时，主机向其同步已有网络对象。</summary>
        private void OnPeerConnected(long id)
        {
            if (!NetCore.Instance.IsHost) return;

            foreach (var kvp in _netObjects)
            {
                if (kvp.Key.UserID == id) continue;

                var prefab = NetObjectManager.Instance.GetPrefab(kvp.Value.PrefabHash);

                RpcId(id, nameof(Rpc_SyncToPeer), kvp.Key.UserID, kvp.Key.ID, kvp.Value.PrefabHash, kvp.Value.Position, kvp.Value.Rotation);
            }

            CatLog.Net($"[{NetCore.Instance.LocalPeerID}][NetObjectRegistry] 已向客户端 {id} 补发 {_netObjects.Count} 个对象");
        }


        /// <summary>注：向新客户端同步网络对象信息，触发对象生成事件。</summary>
        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
        private void Rpc_SyncToPeer(long userId, uint objId, int hash, Vector3 pos, Vector3 rot)
        {
            NetID id = new(userId, objId);
            if (_netObjects.ContainsKey(id)) return;

            var netobj = new NetObject(id, pos, rot, hash, userId);

            _netObjects[id] = netobj;

            OnSpawned?.Invoke(id, null);   
        }

        /// <summary>注：根据网络对象 ID 获取网络对象。</summary>
        public NetObject GetNetObject(NetID id) => _netObjects.TryGetValue(id, out var netobj) ? netobj : null;
    }
}