using System;
using System.Collections.Generic;
using System.Text;
using Godot;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.NetWork
{
    /// <summary>
    /// 分布式对象登记处（对应 Valheim 的 ZDOMan）。
    /// 负责 NetObj 的创建/销毁、数据同步调度、对象 RPC 路由、以及相关事件。
    /// </summary>
    public partial class NetObjectRegistry : Node
    {
        public static NetObjectRegistry Instance { get; set; }

        // ── 核心数据 ──
        private readonly Dictionary<NetID, NetObject> _objects = new();
        private uint _nextObjID = 1;
        private readonly Dictionary<long, PeerSyncState> _peerStates = new();

        // ── 同步调度 ──
        private float _syncTimer;
        private const float SyncInterval = 0.05f;   // 20 Hz

        // ── 对象 RPC 映射 ──
        private readonly Dictionary<NetID, NetSyncBase> _syncBaseMap = new();

        // ── 事件 ──
        public event Action<NetID> OnSpawned;
        public event Action<NetID> OnDestroyed;
        public event Action<NetID> OnDataChanged;

        // NetCore 引用（由 GameCore 注入）
        public NetCore NetCore { private get; set; }

        /// <summary>注：节点初始化，赋值单例并注册RPC处理器</summary>
        public override void _Ready()
        {
            Instance = this;
            // 注册 NetCore 的 RPC 处理器
            NetCore.Instance.RegisterRpcHandler("ObjCreate", OnObjCreate);
            NetCore.Instance.RegisterRpcHandler("ObjDestroy", OnObjDestroy);
            NetCore.Instance.RegisterRpcHandler("ObjSync", OnObjSync);
            NetCore.Instance.RegisterRpcHandler("ObjRpc", OnObjRpc);
        }

        /// <summary>注：每帧更新，定时执行对象同步数据发送</summary>
        public override void _Process(double delta)
        {
            _syncTimer += (float)delta;
            if (_syncTimer >= SyncInterval)
            {
                _syncTimer = 0f;
                SendObjUpdates();
            }
        }

        //══════════════════════════════════════════════════
        //  公开接口：创建/销毁/查询
        //══════════════════════════════════════════════════
        /// <summary>注：创建网络对象，分配ID并广播创建消息</summary>
        public NetObject Spawn(string prefabName, Vector3 pos, Quaternion rot, long owner = -1)
        {
            if (owner == -1) owner = NetCore.LocalPeerID;
            int prefabHash = prefabName.GetStableHashCode();
            var id = new NetID(NetCore.LocalPeerID, _nextObjID++);
            var netobj = new NetObject(id, pos, rot, prefabHash, owner);
            _objects[id] = netobj;

            byte[] payload = SerializeCreate(netobj);
            NetCore.BroadcastRpc("ObjCreate", payload);

            return netobj;
        }

        /// <summary>注：销毁网络对象，移除本地记录并广播销毁消息</summary>
        public void Destroy(NetObject netobj)
        {
            if (netobj == null || !_objects.ContainsKey(netobj.Id)) return;
            _objects.Remove(netobj.Id);

            byte[] payload = SerializeNetID(netobj.Id);
            NetCore.BroadcastRpc("ObjDestroy", payload);
        }

        /// <summary>注：根据NetID获取对应的网络对象</summary>
        public NetObject GetNetObj(NetID id) => _objects.TryGetValue(id, out var netobj) ? netobj : null;

        /// <summary>注：判断本地客户端是否为对象所有者</summary>
        public bool IsOwner(NetID id) => _objects.TryGetValue(id, out var netobj) && netobj.IsOwner(NetCore.LocalPeerID);

        //══════════════════════════════════════════════════
        //  NetSyncBase 映射管理
        //══════════════════════════════════════════════════
        /// <summary>注：注册NetSyncBase组件，建立ID映射</summary>
        public void RegisterNetSyncBase(NetSyncBase syncBase)
        {
            _syncBaseMap[syncBase.NetID] = syncBase;
        }

        /// <summary>注：注销NetSyncBase组件，移除ID映射</summary>
        public void UnregisterNetSyncBase(NetSyncBase syncBase)
        {
            _syncBaseMap.Remove(syncBase.NetID);
        }

        //══════════════════════════════════════════════════
        //  内部 RPC 处理（被 NetCore 回调）
        //══════════════════════════════════════════════════
        /// <summary>注：处理对象创建RPC消息，本地生成对象</summary>
        private void OnObjCreate(long sender, byte[] payload)
        {
            var (id, prefabHash, pos, rot, owner) = DeserializeCreate(payload);
            var netobj = new NetObject(id, pos, rot, prefabHash, owner);
            _objects[id] = netobj;
            OnSpawned?.Invoke(id);
        }

        /// <summary>注：处理对象销毁RPC消息，本地移除对象</summary>
        private void OnObjDestroy(long sender, byte[] payload)
        {
            var id = DeserializeNetID(payload);
            _objects.Remove(id);
            OnDestroyed?.Invoke(id);
        }

        /// <summary>注：处理对象同步RPC消息，更新对象数据</summary>
        private void OnObjSync(long sender, byte[] payload)
        {
            var updates = DeserializeSyncData(payload);
            foreach (var (id, pos, rot, dataRev, ownerRev, ownerId, varsData) in updates)
            {
                if (_objects.TryGetValue(id, out var netobj))
                {
                    netobj.Position = pos;
                    netobj.Rotation = rot;
                    netobj.DataRevision = dataRev;
                    netobj.OwnerRevision = ownerRev;
                    netobj.OwnerPeerID = ownerId;
                    OnDataChanged?.Invoke(id);
                }
            }
        }

        /// <summary>注：处理对象RPC消息，转发给对应组件执行</summary>
        private void OnObjRpc(long sender, byte[] payload)
        {
            using var stream = new System.IO.MemoryStream(payload);
            using var reader = new System.IO.BinaryReader(stream);
            var netId = new NetID(reader.ReadInt64(), reader.ReadUInt32());
            string methodName = reader.ReadString();
            int argsLen = reader.ReadInt32();
            byte[] args = reader.ReadBytes(argsLen);

            if (_syncBaseMap.TryGetValue(netId, out var syncBase))
                syncBase.ReceiveObjectRpc(sender, methodName, args);
        }

        //══════════════════════════════════════════════════
        //  同步调度
        //══════════════════════════════════════════════════
        /// <summary>注：向所有客户端发送变更的对象同步数据</summary>
        private void SendObjUpdates()
        {
            if (NetCore.Multiplayer.MultiplayerPeer == null) return;

            foreach (int peerId in NetCore.Multiplayer.GetPeers())
            {
                if (!_peerStates.TryGetValue(peerId, out var state))
                {
                    state = new PeerSyncState { PeerID = peerId };
                    _peerStates[peerId] = state;
                }

                var dirty = new List<NetObject>();
                foreach (var netobj in _objects.Values)
                {
                    if (!state.KnownRevisions.TryGetValue(netobj.Id, out var known))
                    {
                        dirty.Add(netobj);
                        continue;
                    }
                    if (netobj.DataRevision > known.DataRev || netobj.OwnerRevision > known.OwnerRev)
                        dirty.Add(netobj);
                }

                if (dirty.Count == 0) continue;

                byte[] payload = SerializeSyncData(dirty);
                NetCore.SendRpcToPeer(peerId, "ObjSync", payload);

                foreach (var netobj in dirty)
                    state.KnownRevisions[netobj.Id] = (netobj.DataRevision, netobj.OwnerRevision);
            }
        }

        //══════════════════════════════════════════════════
        //  序列化（多对象支持）
        //══════════════════════════════════════════════════
        /// <summary>注：序列化对象创建数据</summary>
        private byte[] SerializeCreate(NetObject netobj)
        {
            using var stream = new System.IO.MemoryStream();
            using var writer = new System.IO.BinaryWriter(stream);
            writer.Write(netobj.Id.UserID);
            writer.Write(netobj.Id.ID);
            writer.Write(netobj.PrefabHash);
            writer.Write(netobj.Position.X); writer.Write(netobj.Position.Y); writer.Write(netobj.Position.Z);
            writer.Write(netobj.Rotation.X); writer.Write(netobj.Rotation.Y); writer.Write(netobj.Rotation.Z); writer.Write(netobj.Rotation.W);
            writer.Write(netobj.OwnerPeerID);
            return stream.ToArray();
        }

        /// <summary>注：反序列化对象创建数据</summary>
        private (NetID id, int prefabHash, Vector3 pos, Quaternion rot, long owner) DeserializeCreate(byte[] data)
        {
            using var stream = new System.IO.MemoryStream(data);
            using var reader = new System.IO.BinaryReader(stream);
            var id = new NetID(reader.ReadInt64(), reader.ReadUInt32());
            int prefabHash = reader.ReadInt32();
            var pos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            var rot = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            long owner = reader.ReadInt64();
            return (id, prefabHash, pos, rot, owner);
        }

        /// <summary>注：序列化NetID数据</summary>
        private byte[] SerializeNetID(NetID id)
        {
            using var stream = new System.IO.MemoryStream();
            using var writer = new System.IO.BinaryWriter(stream);
            writer.Write(id.UserID);
            writer.Write(id.ID);
            return stream.ToArray();
        }

        /// <summary>注：反序列化NetID数据</summary>
        private NetID DeserializeNetID(byte[] data)
        {
            using var stream = new System.IO.MemoryStream(data);
            using var reader = new System.IO.BinaryReader(stream);
            return new NetID(reader.ReadInt64(), reader.ReadUInt32());
        }

        /// <summary>注：序列化对象同步数据列表</summary>
        private byte[] SerializeSyncData(List<NetObject> list)
        {
            using var stream = new System.IO.MemoryStream();
            using var writer = new System.IO.BinaryWriter(stream);
            writer.Write(list.Count);
            foreach (var netobj in list)
            {
                writer.Write(netobj.Id.UserID);
                writer.Write(netobj.Id.ID);
                writer.Write(netobj.Position.X); writer.Write(netobj.Position.Y); writer.Write(netobj.Position.Z);
                writer.Write(netobj.Rotation.X); writer.Write(netobj.Rotation.Y); writer.Write(netobj.Rotation.Z); writer.Write(netobj.Rotation.W);
                writer.Write(netobj.DataRevision);
                writer.Write(netobj.OwnerRevision);
                writer.Write(netobj.OwnerPeerID);
            }
            return stream.ToArray();
        }

        /// <summary>注：反序列化对象同步数据列表</summary>
        private List<(NetID id, Vector3 pos, Quaternion rot, uint dataRev, ushort ownerRev, long ownerId, byte[] vars)>
            DeserializeSyncData(byte[] data)
        {
            var results = new List<(NetID, Vector3, Quaternion, uint, ushort, long, byte[])>();
            using var stream = new System.IO.MemoryStream(data);
            using var reader = new System.IO.BinaryReader(stream);
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var id = new NetID(reader.ReadInt64(), reader.ReadUInt32());
                var pos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                var rot = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                uint dataRev = reader.ReadUInt32();
                ushort ownerRev = reader.ReadUInt16();
                long ownerId = reader.ReadInt64();
                byte[] vars = null;
                results.Add((id, pos, rot, dataRev, ownerRev, ownerId, vars));
            }
            return results;
        }
    }
}