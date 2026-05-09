using System;
using System.Collections.Generic;
using System.Text;
using Godot;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;   // 稳定哈希扩展方法

public partial class NetCore : Node
{
    public static NetCore Instance { get; set; }

    public const int m_Port = 3043;
    public const int u_Port = 3044;
    public const int Max_Player = 4;

    public bool IsHost => Multiplayer.IsServer();
    public bool IsClient => !Multiplayer.IsServer();

    // 字段
    private readonly Dictionary<NetID, NetSyncBase> _syncBaseMap = new();
    //─────────────── LAN 发现 ───────────────
    private PacketPeerUdp broadcastSender;
    private PacketPeerUdp broadcastListener;
    private float broadcastTimer;
    private bool isBroadcasting;
    private bool isListening;

    private string roomName = "我的房间";
    private int roomPlayerCount = 1;
    private int roomMaxPlayers = Max_Player;

    [Signal] public delegate void RoomFoundEventHandler(string roomName, string ip, int port, int playerCount, int maxPlayers);

    //─────────────── 分布式对象同步 ───────────────
    private readonly Dictionary<NetID, NetObject> _objects = new();
    private readonly Dictionary<int, string> _prefabHashToPath = new();
    private uint _nextObjID = 1;
    private readonly Dictionary<long, PeerSyncState> _peerStates = new();
    private readonly Dictionary<string, Action<long, byte[]>> _rpcHandlers = new();

    private float _syncTimer = 0f;
    private const float SyncInterval = 0.05f;   // 20 Hz

    // 事件
    public event Action<NetID> OnSpawned;
    public event Action<NetID> OnDestroyed;
    public event Action<NetID> OnDataChanged;

    public long LocalPeerID => (long)Multiplayer.GetUniqueId(); // 简化：直接使用 Godot 分配的 ID

    //══════════════════════════════════════════════════
    //  生命周期
    //══════════════════════════════════════════════════
    public override void _Ready()
    {
        Instance = this;

        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;

        RegisterRpcHandler("ObjCreate", OnObjCreate);
        RegisterRpcHandler("ObjDestroy", OnObjDestroy);
        RegisterRpcHandler("ObjSync", OnObjSync);
        RegisterRpcHandler("ObjRpc", OnObjRpc);

        GD.Print("[NetCore]：已完成初始化（含分布式对象管理）");
    }

    public override void _Process(double delta)
    {
        // LAN 广播/监听
        if (isBroadcasting && broadcastSender != null)
        {
            broadcastTimer += (float)delta;
            if (broadcastTimer >= 2.0f)
            {
                broadcastTimer = 0f;
                SendBroadcastPacket();
            }
        }

        if (isListening && broadcastListener != null)
        {
            while (broadcastListener.GetAvailablePacketCount() > 0)
            {
                byte[] bytes = broadcastListener.GetPacket();
                string ip = broadcastListener.GetPacketIP();
                ProcessBroadcastData(bytes, ip);
            }
        }

        // 同步调度
        _syncTimer += (float)delta;
        if (_syncTimer >= SyncInterval)
        {
            _syncTimer = 0f;
            SendObjUpdates();
        }

        // 接收自定义 RPC 包
        ReceiveCustomPackets();
    }

    //══════════════════════════════════════════════════
    //  预制体注册
    //══════════════════════════════════════════════════
    public void RegisterPrefab(string path)
    {
        int hash = PathToHash(path);
        _prefabHashToPath[hash] = path;
    }

    //══════════════════════════════════════════════════
    //  创建与销毁网络对象
    //══════════════════════════════════════════════════
    public NetObject Spawn(string prefabPath, Vector3 pos, Quaternion rot, long owner = -1)
    {
        if (owner == -1) owner = LocalPeerID;

        int prefabHash = PathToHash(prefabPath);
        var id = new NetID(LocalPeerID, _nextObjID++);
        var netobj = new NetObject(id, pos, rot, prefabHash, owner);
        _objects[id] = netobj;

        byte[] payload = SerializeCreate(netobj);
        BroadcastRpc("ObjCreate", payload);

        return netobj;
    }

    public void Destroy(NetObject netobj)
    {
        if (netobj == null || !_objects.ContainsKey(netobj.Id)) return;
        _objects.Remove(netobj.Id);

        byte[] payload = SerializeNetID(netobj.Id);
        BroadcastRpc("ObjDestroy", payload);
    }

    public NetObject GetNetObj(NetID id) => _objects.TryGetValue(id, out var netobj) ? netobj : null;

    public bool IsOwner(NetID id) => _objects.TryGetValue(id, out var netobj) && netobj.IsOwner(LocalPeerID);

    //══════════════════════════════════════════════════
    //  RPC 路由
    //══════════════════════════════════════════════════
    public void RegisterRpcHandler(string method, Action<long, byte[]> handler)
    {
        _rpcHandlers[method] = handler;
    }

    public void SendRpcToPeer(long peerId, string method, byte[] payload)
    {
        var enetPeer = Multiplayer.MultiplayerPeer as ENetMultiplayerPeer;
        if (enetPeer == null)
        {
            GD.PrintErr("NetCore: 当前 MultiplayerPeer 不是 ENetMultiplayerPeer");
            return;
        }

        var packetPeer = enetPeer.GetPeer((int)peerId);
        if (packetPeer == null)
        {
            GD.PrintErr($"NetCore: 未找到 Peer {peerId}");
            return;
        }

        byte[] packet = BuildRpcPacket(method, payload);
        packetPeer.PutPacket(packet);
    }

    public void BroadcastRpc(string method, byte[] payload)
    {
        var enetPeer = Multiplayer.MultiplayerPeer as ENetMultiplayerPeer;
        if (enetPeer == null)
        {
            GD.PrintErr("NetCore: 当前 MultiplayerPeer 不是 ENetMultiplayerPeer");
            return;
        }

        byte[] packet = BuildRpcPacket(method, payload);
        foreach (int peerId in Multiplayer.GetPeers())
        {
            var packetPeer = enetPeer.GetPeer(peerId);
            packetPeer?.PutPacket(packet);
        }
    }

    private void OnRpcReceived(long senderId, byte[] data)
    {
        string method = ExtractMethod(data);
        byte[] payload = ExtractPayload(data);
        if (_rpcHandlers.TryGetValue(method, out var handler))
            handler(senderId, payload);
        else
            GD.PrintErr($"未注册的 RPC 方法: {method}");
    }

    private void ReceiveCustomPackets()
    {
        var enetPeer = Multiplayer.MultiplayerPeer as ENetMultiplayerPeer;
        if (enetPeer == null) return;

        if (IsHost)
        {
            foreach (int peerId in Multiplayer.GetPeers())
            {
                var packetPeer = enetPeer.GetPeer(peerId);
                while (packetPeer != null && packetPeer.GetAvailablePacketCount() > 0)
                {
                    byte[] data = packetPeer.GetPacket();
                    OnRpcReceived(peerId, data);
                }
            }
        }
        else
        {
            var packetPeer = enetPeer.GetPeer(1);
            while (packetPeer != null && packetPeer.GetAvailablePacketCount() > 0)
            {
                byte[] data = packetPeer.GetPacket();
                OnRpcReceived(1, data);
            }
        }
    }

    //══════════════════════════════════════════════════
    //  内部 RPC 处理
    //══════════════════════════════════════════════════
    private void OnObjCreate(long sender, byte[] payload)
    {
        var (id, prefabHash, pos, rot, owner) = DeserializeCreate(payload);
        var netobj = new NetObject(id, pos, rot, prefabHash, owner);
        _objects[id] = netobj;
        OnSpawned?.Invoke(id);
    }

    private void OnObjDestroy(long sender, byte[] payload)
    {
        var id = DeserializeNetID(payload);
        _objects.Remove(id);
        OnDestroyed?.Invoke(id);
    }

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
                // 如有 Vars 数据此处可反序列化合并
                OnDataChanged?.Invoke(id);
            }
        }
    }

    //══════════════════════════════════════════════════
    //  同步调度（支持多对象）
    //══════════════════════════════════════════════════
    private void SendObjUpdates()
    {
        if (Multiplayer.MultiplayerPeer == null) return;

        foreach (int peerId in Multiplayer.GetPeers())
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
            SendRpcToPeer(peerId, "ObjSync", payload);

            foreach (var netobj in dirty)
                state.KnownRevisions[netobj.Id] = (netobj.DataRevision, netobj.OwnerRevision);
        }
    }

    //══════════════════════════════════════════════════
    //  稳定哈希（使用 CatUtils.GetStableHashCode）
    //══════════════════════════════════════════════════
    private static int PathToHash(string path) => path.GetStableHashCode();

    //══════════════════════════════════════════════════
    //  序列化（支持多对象）
    //══════════════════════════════════════════════════
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

    private byte[] SerializeNetID(NetID id)
    {
        using var stream = new System.IO.MemoryStream();
        using var writer = new System.IO.BinaryWriter(stream);
        writer.Write(id.UserID);
        writer.Write(id.ID);
        return stream.ToArray();
    }

    private NetID DeserializeNetID(byte[] data)
    {
        using var stream = new System.IO.MemoryStream(data);
        using var reader = new System.IO.BinaryReader(stream);
        return new NetID(reader.ReadInt64(), reader.ReadUInt32());
    }

    private byte[] SerializeSyncData(List<NetObject> list)
    {
        using var stream = new System.IO.MemoryStream();
        using var writer = new System.IO.BinaryWriter(stream);
        writer.Write(list.Count);  // 对象数量
        foreach (var netobj in list)
        {
            writer.Write(netobj.Id.UserID);
            writer.Write(netobj.Id.ID);
            writer.Write(netobj.Position.X); writer.Write(netobj.Position.Y); writer.Write(netobj.Position.Z);
            writer.Write(netobj.Rotation.X); writer.Write(netobj.Rotation.Y); writer.Write(netobj.Rotation.Z); writer.Write(netobj.Rotation.W);
            writer.Write(netobj.DataRevision);
            writer.Write(netobj.OwnerRevision);
            writer.Write(netobj.OwnerPeerID);
            // Vars 可后续扩展
        }
        return stream.ToArray();
    }

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
            // vars 预留
            byte[] vars = null;
            results.Add((id, pos, rot, dataRev, ownerRev, ownerId, vars));
        }
        return results;
    }

    //══════════════════════════════════════════════════
    //  RPC 包构建与解析
    //══════════════════════════════════════════════════
    private byte[] BuildRpcPacket(string method, byte[] payload)
    {
        byte[] methodBytes = Encoding.UTF8.GetBytes(method);
        using var stream = new System.IO.MemoryStream();
        using var writer = new System.IO.BinaryWriter(stream);
        writer.Write(methodBytes.Length);
        writer.Write(methodBytes);
        writer.Write(payload.Length);
        writer.Write(payload);
        return stream.ToArray();
    }

    private string ExtractMethod(byte[] packet)
    {
        using var stream = new System.IO.MemoryStream(packet);
        using var reader = new System.IO.BinaryReader(stream);
        int len = reader.ReadInt32();
        return Encoding.UTF8.GetString(reader.ReadBytes(len));
    }

    private byte[] ExtractPayload(byte[] packet)
    {
        using var stream = new System.IO.MemoryStream(packet);
        using var reader = new System.IO.BinaryReader(stream);
        int methodLen = reader.ReadInt32();
        stream.Seek(methodLen, System.IO.SeekOrigin.Current);
        int payloadLen = reader.ReadInt32();
        return reader.ReadBytes(payloadLen);
    }

    //══════════════════════════════════════════════════
    //  连接回调
    //══════════════════════════════════════════════════
    private void OnPeerConnected(long id)
    {
        GD.Print($"[NetCore] 玩家连接: {id}");
        _peerStates[id] = new PeerSyncState { PeerID = id };
    }

    private void OnPeerDisconnected(long id)
    {
        GD.Print($"[NetCore] 玩家断开: {id}");
        _peerStates.Remove(id);
    }

    private void OnConnectedToServer()
    {
        GD.Print("[NetCore] 成功连接服务器");
    }

    private void OnConnectionFailed()
    {
        GD.Print("[NetCore] 连接失败");
    }

    //══════════════════════════════════════════════════
    //  LAN 发现
    //══════════════════════════════════════════════════
    public Error StartLANHost()
    {
        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        Error err = peer.CreateServer(m_Port, Max_Player);
        if (err != Error.Ok) return err;

        Multiplayer.MultiplayerPeer = peer;
        GD.Print("[NetworkCore] 已创建局域网房间");
        return Error.Ok;
    }

    public Error JoinLAN(string ipAddress, int port = m_Port)
    {
        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        Error err = peer.CreateClient(ipAddress, port);
        if (err != Error.Ok) return err;

        Multiplayer.MultiplayerPeer = peer;
        GD.Print($"[NetworkCore] 正在连接: {ipAddress}:{port}");
        return Error.Ok;
    }

    public void Disconnect()
    {
        StopBroadcast();
        StopListening();
        if (Multiplayer.MultiplayerPeer != null)
        {
            Multiplayer.MultiplayerPeer.Close();
            Multiplayer.MultiplayerPeer = null;
        }
        _objects.Clear();
        _peerStates.Clear();
    }

    public void StartBroadcast(string name, int currentPlayers, int maxPlayers)
    {
        if (isBroadcasting) return;
        roomName = name;
        roomPlayerCount = currentPlayers;
        roomMaxPlayers = maxPlayers;

        broadcastSender = new PacketPeerUdp();
        broadcastSender.SetBroadcastEnabled(true);
        broadcastSender.SetDestAddress("255.255.255.255", u_Port);
        isBroadcasting = true;
        broadcastTimer = 0f;
        GD.Print("[NetworkCore] 开始广播房间");
    }

    public void StopBroadcast()
    {
        if (!isBroadcasting) return;
        isBroadcasting = false;
        broadcastSender?.Close();
        broadcastSender = null;
    }

    private void SendBroadcastPacket()
    {
        var data = new Godot.Collections.Dictionary
        {
            { "type", "room_info" },
            { "name", roomName },
            { "playerCount", roomPlayerCount },
            { "maxPlayers", roomMaxPlayers },
            { "gamePort", m_Port }
        };
        string json = Json.Stringify(data);
        broadcastSender.PutPacket(Encoding.UTF8.GetBytes(json));
    }

    public void StartListening()
    {
        if (isListening) return;
        broadcastListener = new PacketPeerUdp();
        broadcastListener.Bind(u_Port);
        isListening = true;
        GD.Print("[NetworkCore] 开始监听房间广播");
    }

    public void StopListening()
    {
        if (!isListening) return;
        isListening = false;
        broadcastListener?.Close();
        broadcastListener = null;
    }

    private void ProcessBroadcastData(byte[] bytes, string senderIP)
    {
        string json = Encoding.UTF8.GetString(bytes);
        var data = Json.ParseString(json).AsGodotDictionary();
        if (data == null || (string)data["type"] != "room_info") return;

        string name = (string)data["name"];
        int playerCount = (int)(float)data["playerCount"];
        int maxPlayers = (int)(float)data["maxPlayers"];
        int port = (int)(float)data["gamePort"];

        EmitSignal("RoomFound", name, senderIP, port, playerCount, maxPlayers);
    }

    // 注册/注销 NetSyncBase 映射
    public void RegisterNetSyncBase(NetSyncBase syncBase)
    {
        _syncBaseMap[syncBase.NetID] = syncBase;
    }

    public void UnregisterNetSyncBase(NetSyncBase syncBase)
    {
        _syncBaseMap.Remove(syncBase.NetID);
    }

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

    public string GetPrefabPathByHash(int hash)
    {
        _prefabHashToPath.TryGetValue(hash, out var path);
        return path;
    }
}

// 辅助类
public class PeerSyncState
{
    public long PeerID;
    public Dictionary<NetID, (uint DataRev, ushort OwnerRev)> KnownRevisions = new();
    public HashSet<NetID> ForceSendQueue = new();
}