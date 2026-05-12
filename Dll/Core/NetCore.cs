using Godot;
using System.Numerics;
using System.Text;
using 途畔归所.Dll.Manager;

public partial class NetCore : Node
{
	private static NetCore _instance;
	public static NetCore Instance { get => _instance ??= new(); set => _instance ??= value; }

	private const int m_Port = 3043;
	private const int u_Port = 3044;
	public const int Max_Player = 4;


    public const long ServerID = 1; // Godot 服务器 Peer ID 固定为 1


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
	public int LocalPeerID => Multiplayer.GetUniqueId();

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
	}

	#region 房间创建与加入
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







	private void OnPeerConnected(long id) => GD.Print($"[NetCore] 玩家连接: {id}");

	private void OnPeerDisconnected(long id)
	{
		GD.Print($"[NetObjectRegistry] Peer {id} 断开连接，尚未实现清理逻辑");

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
}
