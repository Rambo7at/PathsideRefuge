using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;

public partial class NetCore : Node
{
    private static NetCore _instance;

    public static NetCore Instance { get => _instance??= new NetCore(); set => _instance ??= value; }

    private const int m_Port = 3043;
    private const int u_Port = 3044;
    public const int Max_Player = 4;

    public bool IsHost => Multiplayer.IsServer();
    public bool IsClient => !Multiplayer.IsServer();

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

    //─────────────── RPC 路由 ───────────────
    private readonly Dictionary<string, Action<long, byte[]>> _rpcHandlers = new();

    public long LocalPeerID => (long)Multiplayer.GetUniqueId();

    /// <summary>注：节点初始化，绑定网络事件并设置单例</summary>
    public override void _Ready()
    {
        Instance = this;
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;

        GD.Print("[NetCore]：已完成初始化（通信层）");
    }

    /// <summary>注：每帧更新，处理局域网广播、监听和自定义RPC接收</summary>
    public override void _Process(double delta)
    {
        // LAN 广播
        if (isBroadcasting && broadcastSender != null)
        {
            broadcastTimer += (float)delta;
            if (broadcastTimer >= 2.0f)
            {
                broadcastTimer = 0f;
                SendBroadcastPacket();
            }
        }

        // LAN 监听
        if (isListening && broadcastListener != null)
        {
            while (broadcastListener.GetAvailablePacketCount() > 0)
            {
                byte[] bytes = broadcastListener.GetPacket();
                string ip = broadcastListener.GetPacketIP();
                ProcessBroadcastData(bytes, ip);
            }
        }

        // 接收自定义 RPC 包
        ReceiveCustomPackets();
    }

    #region RPC 核心方法

    /// <summary>注：注册RPC方法处理器</summary>
    public void RegisterRpcHandler(string method, Action<long, byte[]> handler)
    {
        _rpcHandlers[method] = handler;
    }

    /// <summary>注：向指定客户端发送RPC消息</summary>
    public void SendRpcToPeer(long peerId, string method, byte[] payload)
    {
        var enetPeer = Multiplayer.MultiplayerPeer as ENetMultiplayerPeer;
        if (enetPeer == null) return;

        var packetPeer = enetPeer.GetPeer((int)peerId);
        if (packetPeer == null) return;

        byte[] packet = BuildRpcPacket(method, payload);
        packetPeer.PutPacket(packet);
    }

    /// <summary>注：向所有客户端广播RPC消息</summary>
    public void BroadcastRpc(string method, byte[] payload)
    {
        var enetPeer = Multiplayer.MultiplayerPeer as ENetMultiplayerPeer;
        if (enetPeer == null) return;

        byte[] packet = BuildRpcPacket(method, payload);
        foreach (int peerId in Multiplayer.GetPeers())
        {
            var packetPeer = enetPeer.GetPeer(peerId);
            packetPeer?.PutPacket(packet);
        }
    }

    /// <summary>注：处理接收到的RPC消息，分发到对应处理器</summary>
    private void OnRpcReceived(long senderId, byte[] data)
    {
        string method = ExtractMethod(data);
        byte[] payload = ExtractPayload(data);
        if (_rpcHandlers.TryGetValue(method, out var handler))
            handler(senderId, payload);
        else
            GD.PrintErr($"未注册的 RPC 方法: {method}");
    }

    /// <summary>注：接收并处理所有客户端的自定义网络包</summary>
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

    #endregion

    #region RPC 包构建/解析

    /// <summary>注：构建RPC数据包，包含方法名和载荷</summary>
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

    /// <summary>注：从RPC包中解析出方法名</summary>
    private string ExtractMethod(byte[] packet)
    {
        using var stream = new System.IO.MemoryStream(packet);
        using var reader = new System.IO.BinaryReader(stream);
        int len = reader.ReadInt32();
        return Encoding.UTF8.GetString(reader.ReadBytes(len));
    }

    /// <summary>注：从RPC包中解析出数据载荷</summary>
    private byte[] ExtractPayload(byte[] packet)
    {
        using var stream = new System.IO.MemoryStream(packet);
        using var reader = new System.IO.BinaryReader(stream);
        int methodLen = reader.ReadInt32();
        stream.Seek(methodLen, System.IO.SeekOrigin.Current);
        int payloadLen = reader.ReadInt32();
        return reader.ReadBytes(payloadLen);
    }

    #endregion

    #region LAN 发现
    /// <summary>注：启动局域网主机，创建服务器</summary>
    public Error StartLANHost()
    {
        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        Error err = peer.CreateServer(m_Port, Max_Player);
        if (err != Error.Ok) return err;
        Multiplayer.MultiplayerPeer = peer;
        GD.Print("[NetCore] 已创建局域网房间");
        return Error.Ok;
    }

    /// <summary>注：加入指定IP的局域网房间</summary>
    public Error JoinLAN(string ipAddress, int port = m_Port)
    {
        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        Error err = peer.CreateClient(ipAddress, port);
        if (err != Error.Ok) return err;
        Multiplayer.MultiplayerPeer = peer;
        GD.Print($"[NetCore] 正在连接: {ipAddress}:{port}");
        return Error.Ok;
    }

    /// <summary>注：断开网络连接，清理所有网络资源</summary>
    public void Disconnect()
    {
        StopBroadcast();
        StopListening();
        if (Multiplayer.MultiplayerPeer != null)
        {
            Multiplayer.MultiplayerPeer.Close();
            Multiplayer.MultiplayerPeer = null;
        }
        NetObjectRegistry.Instance?.ClearAll();
    }

    /// <summary>注：启动局域网房间广播</summary>
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
    }

    /// <summary>注：停止局域网房间广播</summary>
    public void StopBroadcast()
    {
        if (!isBroadcasting) return;
        isBroadcasting = false;
        broadcastSender?.Close();
        broadcastSender = null;
    }

    /// <summary>注：发送局域网房间信息广播包</summary>
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

    /// <summary>注：启动局域网房间监听</summary>
    public void StartListening()
    {
        if (isListening) return;
        broadcastListener = new PacketPeerUdp();
        broadcastListener.Bind(u_Port);
        isListening = true;
    }

    /// <summary>注：停止局域网房间监听</summary>
    public void StopListening()
    {
        if (!isListening) return;
        isListening = false;
        broadcastListener?.Close();
        broadcastListener = null;
    }

    /// <summary>注：处理接收到的局域网广播数据</summary>
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

    #endregion

    #region  连接回调（转发给 NetObjectRegistry 的部分由外部监听事件处理）
    /// <summary>注：玩家连接时触发，通知对象管理器</summary>
    private void OnPeerConnected(long id)
    {
        GD.Print($"[NetCore] 玩家连接: {id}");
        NetObjectRegistry.Instance?.OnPeerConnected(id);

        // ✅ 主机为新加入的客户端生成玩家角色
        if (IsHost)
        {
            PlayerManager.Instance?.SpawnPlayerForPeer(id);
        }
    }

    /// <summary>注：玩家断开时触发，通知对象管理器</summary>
    private void OnPeerDisconnected(long id)
    {
        GD.Print($"[NetCore] 玩家断开: {id}");
        NetObjectRegistry.Instance?.OnPeerDisconnected(id);
    }

    /// <summary>注：成功连接到服务器时触发</summary>
    private void OnConnectedToServer()
    {
        GD.Print("[NetCore] 成功连接服务器");
    }

    /// <summary>注：连接服务器失败时触发</summary>
    private void OnConnectionFailed()
    {
        GD.Print("[NetCore] 连接失败");
    }

    #endregion

}

// 辅助类保留
public class PeerSyncState
{
    public long PeerID;
    public Dictionary<NetID, (uint DataRev, ushort OwnerRev)> KnownRevisions = new();
    public HashSet<NetID> ForceSendQueue = new();
}