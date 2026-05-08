using Godot;
using Godot.Collections;
using System.Text;
using 途畔归所.Dll.Manager;
using Error = Godot.Error;

namespace 途畔归所.Dll.Core
{
    public partial class NetCore : Node
    {
        public static NetCore Instance { get; set; }

        public const int m_Port = 3043;
        public const int u_Port = 3044;
        public const int Max_Player = 4;

        public bool IsHost => Multiplayer.IsServer();
        public bool IsClient => !Multiplayer.IsServer();


        // LAN 发现相关（使用正确的 PacketPeerUdp 类型）
        private PacketPeerUdp broadcastSender;
        private PacketPeerUdp broadcastListener;
        private float broadcastTimer;
        private bool isBroadcasting;
        private bool isListening;

        private string roomName = "我的房间";
        private int roomPlayerCount = 1;
        private int roomMaxPlayers = Max_Player;

        [Signal] public delegate void RoomFoundEventHandler(string roomName, string ip, int port, int playerCount, int maxPlayers);

        public override void _Ready()
        {
            Instance = this;

            Multiplayer.PeerConnected += OnPeerConnected;
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
            Multiplayer.ConnectedToServer += OnConnectedToServer;
            Multiplayer.ConnectionFailed += OnConnectionFailed;

            GD.Print("[NetworkCore]：已完成初始化");
        }

        public override void _Process(double delta)
        {
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

        /// <summary> 注：创建局域网主机，绑定指定端口并设置最大玩家数 </summary>
        public Error StartLANHost()
        {
            ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
            Error err = peer.CreateServer(m_Port, Max_Player);

            if (err != Error.Ok) return err;

            Multiplayer.MultiplayerPeer = peer;


            GD.Print("[NetworkCore] 已创建局域网房间");
            return Error.Ok;
        }

        /// <summary> 注：加入指定IP和端口的局域网房间 </summary>
        public Error JoinLAN(string ipAddress, int port = m_Port)
        {
            ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
            Error err = peer.CreateClient(ipAddress, port);

            if (err != Error.Ok) return err;

            Multiplayer.MultiplayerPeer = peer;
            GD.Print($"[NetworkCore] 正在连接: {ipAddress}:{port}");

            return Error.Ok;
        }

        /// <summary> 注：断开网络连接，停止广播与监听，关闭并清空网络对等体 </summary>
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

        /// <summary> 注：开启主机房间广播，设置房间信息并初始化UDP发送器 </summary>
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

        /// <summary> 注：停止主机房间广播，关闭并清空UDP发送器 </summary>
        public void StopBroadcast()
        {
            if (!isBroadcasting) return;
            isBroadcasting = false;
            broadcastSender?.Close();
            broadcastSender = null;
        }

        /// <summary> 注：发送房间信息的UDP广播包，包含房间名、玩家数等数据 </summary>
        private void SendBroadcastPacket()
        {
            var data = new Dictionary
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

        /// <summary> 注：开启客户端房间监听，绑定指定UDP端口 </summary>
        public void StartListening()
        {
            if (isListening) return;
            broadcastListener = new PacketPeerUdp();
            broadcastListener.Bind(u_Port);
            isListening = true;
            GD.Print("[NetworkCore] 开始监听房间广播");
        }

        /// <summary> 注：停止客户端房间监听，关闭并清空UDP监听器 </summary>
        public void StopListening()
        {
            if (!isListening) return;
            isListening = false;
            broadcastListener?.Close();
            broadcastListener = null;
        }

        /// <summary> 注：解析接收到的UDP广播包，验证类型后触发房间发现信号 </summary>
        private void ProcessBroadcastData(byte[] bytes, string senderIP)
        {
            string json = Encoding.UTF8.GetString(bytes);
            var data = Json.ParseString(json).AsGodotDictionary();
            if (data == null || (string)data["type"] != "room_info") return;

            string name = (string)data["name"];
            int playerCount = (int)(float)data["playerCount"];
            int maxPlayers = (int)(float)data["maxPlayers"];
            int port = (int)(float)data["gamePort"];

            // 使用字符串信号名避免 SignalName 自动生成问题
            EmitSignal("RoomFound", name, senderIP, port, playerCount, maxPlayers);
        }





        protected virtual void OnPeerConnected(long id) => GD.Print($"[NetCore] 玩家连接: {id}");

        /// <summary> 注：玩家断开连接的回调，打印日志 </summary>
        protected virtual void OnPeerDisconnected(long id) => GD.Print($"[NetCore] 玩家断开: {id}");
        /// <summary> 注：成功连接到服务器的回调，打印日志 </summary>
        protected virtual void OnConnectedToServer() => GD.Print("[NetCore] 成功连接服务器");
        /// <summary> 注：连接服务器失败的回调，打印日志 </summary>
        protected virtual void OnConnectionFailed() => GD.Print("[NetCore] 连接失败");
    }
}