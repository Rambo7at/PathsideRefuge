using Godot;

namespace 途畔归所.Dll.Core
{
    public partial class NetworkCore : Node
    {
        public static NetworkCore Instance { get; private set; }

        /// <summary>注：端口 </summary>
        public const int Default_Port = 3043;
        /// <summary>注：最大玩家 </summary>
        public const int Max_Player = 4;

        // 状态属性（Godot 4 正确写法）
        public bool IsHost => Multiplayer.IsServer();
        public bool IsClient => !Multiplayer.IsServer(); 
        public new bool IsConnected => Multiplayer.MultiplayerPeer != null;

        public override void _Ready()
        {
            Instance = this;

            // 网络事件监听
            Multiplayer.PeerConnected += OnPeerConnected;
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
            Multiplayer.ConnectedToServer += OnConnectedToServer;
            Multiplayer.ConnectionFailed += OnConnectionFailed;
        }

        /// <summary>
        /// 创建局域网主机(P2P Host)
        /// </summary>
        public void StartLANHost()
        {
            ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
            peer.CreateServer(Default_Port, Max_Player);
            Multiplayer.MultiplayerPeer = peer;
            GD.Print("[NetworkCore] 已创建局域网房间");
        }

        /// <summary>
        /// 加入局域网主机
        /// </summary>
        public void JoinLAN(string ipAddress)
        {
            ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
            peer.CreateClient(ipAddress, Default_Port); // ✅ 修复：端口，不是最大玩家
            Multiplayer.MultiplayerPeer = peer;
            GD.Print("[NetworkCore] 正在连接: " + ipAddress);
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (Multiplayer.MultiplayerPeer != null)
            {
                Multiplayer.MultiplayerPeer.Close();
                Multiplayer.MultiplayerPeer = null;
            }
        }

        #region 网络事件
        protected virtual void OnPeerConnected(long id) => GD.Print($"[NetCore] 玩家连接: {id}");
        protected virtual void OnPeerDisconnected(long id) => GD.Print($"[NetCore] 玩家断开: {id}");
        protected virtual void OnConnectedToServer() => GD.Print("[NetCore] 成功连接服务器");
        protected virtual void OnConnectionFailed() => GD.Print("[NetCore] 连接失败");
        #endregion
    }
}