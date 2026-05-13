using Godot;
using System;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Core
{
    public partial class NetCore : Node
    {

        private static NetCore _instance;
        public static NetCore Instance { get => _instance ??= new(); set => _instance ??= value; }

        // 公共常量，供外部（包括 LanCore）引用
        public const int m_Port = 3043;
        public const int Max_Player = 4;
        public const long ServerID = 1;

        private LanCore lanCore;

        public bool IsHost => Multiplayer.IsServer();
        public bool IsClient => !IsHost;
        public int LocalPeerID => Multiplayer.GetUniqueId();

        // 暴露房间发现事件（C# 风格，不是 Godot 信号）
        public event Action<string, string, int, int, int> RoomFound;

        public override void _Ready()
        {
            Instance = this;
            Multiplayer.PeerConnected += OnPeerConnected;
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
            Multiplayer.ConnectedToServer += OnConnectedToServer;
            Multiplayer.ConnectionFailed += OnConnectionFailed;

            lanCore = new LanCore();
            lanCore.OnRoomFound += (name, ip, port, players, max) =>
            {
                RoomFound?.Invoke(name, ip, port, players, max);
            };

            CatLog.Ok("[NetCore]：已完成初始化");
        }

        public override void _Process(double delta) => lanCore.Process(delta);

        public Error StartLANHost()
        {
            ENetMultiplayerPeer peer = new();
            Error err = peer.CreateServer(m_Port, Max_Player);
            if (err != Error.Ok) return err;
            Multiplayer.MultiplayerPeer = peer;
            CatLog.Info("[NetCore] 已创建局域网房间");
            return Error.Ok;
        }

        public Error JoinLAN(string ipAddress, int port = m_Port)
        {
            ENetMultiplayerPeer peer = new();
            Error err = peer.CreateClient(ipAddress, port);
            if (err != Error.Ok) return err;
            Multiplayer.MultiplayerPeer = peer;
            CatLog.Info($"[NetCore] 正在连接: {ipAddress}:{port}");
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
            CatLog.Info("[NetCore] 已断开连接");
        }

        #region LAN 发现转发（直接委托给 LanCore）
        public void StartBroadcast(string name, int currentPlayers, int maxPlayers) =>
            lanCore.StartBroadcast(name, currentPlayers, maxPlayers);

        public void StopBroadcast() => lanCore.StopBroadcast();
        public void StartListening() => lanCore.StartListening();
        public void StopListening() => lanCore.StopListening();
        #endregion

        private void OnPeerConnected(long id) => CatLog.Info($"[NetCore] 玩家连接: {id}");
        private void OnPeerDisconnected(long id) => CatLog.Info($"[NetCore] 玩家断开: {id}");
        private void OnConnectedToServer() => CatLog.Info("[NetCore] 成功连接服务器");
        private void OnConnectionFailed() => CatLog.Err("[NetCore] 连接失败");
    }
}