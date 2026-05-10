using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;

public partial class NetCore : Node
{
    private static NetCore _instance;
    public static NetCore Instance { get => _instance ??= new(); set => _instance ??= value; }

    private const int m_Port = 3043;
    private const int u_Port = 3044;
    public const int Max_Player = 4;

    // 判断角色
    public bool IsHost => Multiplayer.IsServer();
    public bool IsClient => !IsHost;

    //─────────────── LAN 发现（保留原有 UDP 广播）───────────────
    private PacketPeerUdp broadcastSender;
    private PacketPeerUdp broadcastListener;
    private float broadcastTimer;
    private bool isBroadcasting;
    private bool isListening;

    private string roomName = "我的房间";
    private int roomPlayerCount = 1;
    private int roomMaxPlayers = Max_Player;

    [Signal]
    public delegate void RoomFoundEventHandler(string roomName, string ip, int port, int playerCount, int maxPlayers);

    //─────────────── 连接管理 ───────────────
    public long LocalPeerID => (long)Multiplayer.GetUniqueId();

    public override void _Ready()
    {
        Instance = this;
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;
        GD.Print("[NetCore]：已完成初始化（原生 RPC 模式）");
    }

    public override void _Process(double delta)
    {
        // LAN 广播逻辑（不变）
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

        // 不再需要 ReceiveCustomPackets，Godot 原生 RPC 自动处理
    }

    #region 房间创建与加入（使用 ENetMultiplayerPeer，原生 RPC 通道 0）
    public Error StartLANHost()
    {
        ENetMultiplayerPeer peer = new();
        Error err = peer.CreateServer(m_Port, Max_Player);
        if (err != Error.Ok) return err;
        Multiplayer.MultiplayerPeer = peer;
        GD.Print("[NetCore] 已创建局域网房间");
        return Error.Ok;
    }

    public Error JoinLAN(string ipAddress, int port = m_Port)
    {
        ENetMultiplayerPeer peer = new();
        Error err = peer.CreateClient(ipAddress, port);
        if (err != Error.Ok) return err;
        Multiplayer.MultiplayerPeer = peer;
        GD.Print($"[NetCore] 正在连接: {ipAddress}:{port}");
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
        NetObjectRegistry.Instance?.ClearAll();
    }
    #endregion

    #region LAN 发现方法（完全不变）
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
    #endregion

    #region 连接回调（现在可作为信号转发，也可由 NetObjectRegistry 监听）
    private void OnPeerConnected(long id)
    {
        GD.Print($"[NetCore] 玩家连接: {id}");
        // 主机为新客户端生成玩家（使用原生 RPC 调用自身方法，确保在主机执行）
        if (IsHost)
        {
            // 延迟一帧调用，确保客户端已完全设置
            CallDeferred(nameof(SpawnPlayerForPeer), id);
        }
    }

    private void OnPeerDisconnected(long id)
    {
        GD.Print($"[NetObjectRegistry] Peer {id} 断开连接，尚未实现清理逻辑");
        //NetObjectRegistry.Instance?.OnPeerDisconnected(id);

    }

    private void OnConnectedToServer()
    {
        GD.Print("[NetCore] 成功连接服务器");
        // 客户端连接后，可在此处触发请求玩家生成（或由 MainWorld 控制）
        // 这里保持为空，留给场景逻辑处理
    }

    private void OnConnectionFailed()
    {
        GD.Print("[NetCore] 连接失败");
    }
    #endregion

    #region 核心 RPC 方法（标签属性，Godot 自动处理序列化）
    /// <summary>
    /// 服务器端：为新加入的 Peer 生成玩家对象（RPC 内部调用 PlayerManager）
    /// 只有服务器可以执行此方法。
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    private void SpawnPlayerForPeer(long peerId)
    {
        // 此处实际上是主机在本地执行（因为方法上有 Authority 标记，且由主机调用）
        GD.Print($"[NetCore] 主机执行：为 Peer {peerId} 生成玩家");
        PlayerManager.Instance.SpawnPlayerForPeer(peerId);
    }

    /// <summary>
    /// 客户端可调用的请求：向服务器申请为自己生成玩家。
    /// 该方法在服务器端执行。
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void RequestSpawnPlayer()
    {
        // 此方法在服务器上被调用，sender 可通过 Multiplayer.GetRemoteSenderId() 获得
        long senderId = (long)Multiplayer.GetRemoteSenderId();
        GD.Print($"[NetCore] 服务器收到 RequestSpawnPlayer，来自 Peer {senderId}");
        SpawnPlayerForPeer(senderId); // 调用自身的另一个 RPC 或直接调用 PlayerManager
    }

    // 你可以继续添加更多 RPC 方法，例如 RequestWorldSync 等
    #endregion
}

// 辅助类 PeerSyncState 暂留
public class PeerSyncState
{
    public long PeerID;
    public Dictionary<NetID, (uint DataRev, ushort OwnerRev)> KnownRevisions = new();
    public HashSet<NetID> ForceSendQueue = new();
}