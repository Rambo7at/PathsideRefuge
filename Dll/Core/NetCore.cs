using Godot;
using System;
using System.Text;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Core
{
    public partial class NetCore : Node
    {
        private static NetCore _instance;
        public static NetCore Instance { get => _instance ??= new(); set => _instance ??= value; }

        private const int m_Port = 3043;
        private const int Max_Player = 4;


        public const long ServerID = 1; // Godot 服务器 Peer ID 固定为 1

        public bool IsHost => Multiplayer.IsServer();
        public bool IsClient => !IsHost;

        //─────────────── LAN 发现（保留原有 UDP 广播）───────────────
        private LanCore m_lanCore;
        public  string m_RoomIP;

        private string roomName = "我的房间";
        private int roomPlayerCount = 1;
        private int roomMaxPlayers = Max_Player;


        //─────────────── 连接管理 ───────────────
        public int LocalPeerID => Multiplayer.GetUniqueId();

        public override void _Ready()
        {
            Instance = this;
            Multiplayer.PeerConnected += OnPeerConnected;
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
            Multiplayer.ConnectedToServer += OnConnectedToServer;
            Multiplayer.ConnectionFailed += OnConnectionFailed;

            CatLog.Ok("[NetCore]：已完成初始化");
        }

        public override void _Process(double delta)
        {
            if (m_lanCore != null)
            {
                m_lanCore.Start(delta);
                m_RoomIP = m_lanCore.LatestRoomIP;
            }
        }

        #region 房间创建与加入
        public Error StartLANHost()
        {
            ENetMultiplayerPeer peer = new();
            Error err = peer.CreateServer(m_Port, Max_Player);
            if (err != Error.Ok) return err;
            Multiplayer.MultiplayerPeer = peer;

            m_lanCore = new LanCore(roomName, Max_Player);

            CatLog.Ok("[NetCore] 已创建局域网房间并开始广播");
            return Error.Ok;
        }

        public Error JoinLAN(string ipAddress)
        {
            ENetMultiplayerPeer peer = new();
            Error err = peer.CreateClient(ipAddress, m_Port);
            if (err != Error.Ok) return err;
            Multiplayer.MultiplayerPeer = peer;
            GD.Print($"[NetCore] 正在连接: {ipAddress}:{m_Port}");
            return Error.Ok;
        }

        public void FindLANRoom()
        {
            if (m_lanCore != null)
            {
                m_lanCore.StopBroadcast();
                m_lanCore.StopListening();
                m_lanCore = null;
            }

            m_lanCore = new LanCore();   // 无参构造自动 StartListening
            CatLog.Info("[NetCore] 开始搜索局域网房间...");
        }

        public void StartLANDiscovery()
        {
            if (m_lanCore != null) return; 
            m_lanCore = new LanCore();     
        }

        public void StopLANDiscovery()
        {
            m_lanCore?.StopListening();
            m_lanCore = null;
        }


        public void Disconnect()
        {
            m_lanCore?.StopBroadcast();
            m_lanCore?.StopListening();
            m_lanCore = null;

            if (Multiplayer.MultiplayerPeer != null)
            {
                Multiplayer.MultiplayerPeer.Close();
                Multiplayer.MultiplayerPeer = null;
            }
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
}