using System;
using Godot;
using Godot.Collections;


namespace 途畔归所.Dll.Core
{
    internal class LanCore
    {
        public event Action<string, string, int, int, int> OnRoomFound;

        private const int BroadcastPort = 3044;
        private PacketPeerUdp broadcastSender;
        private PacketPeerUdp broadcastListener;
        private float broadcastTimer;
        private bool isBroadcasting;
        private bool isListening;

        private string roomName = "我的房间";
        private int roomPlayerCount = 1;
        private int roomMaxPlayers = 4;

        public void StartBroadcast(string name, int currentPlayers, int maxPlayers)
        {
            if (isBroadcasting) return;
            roomName = name;
            roomPlayerCount = currentPlayers;
            roomMaxPlayers = maxPlayers;

            broadcastSender = new PacketPeerUdp();
            broadcastSender.SetBroadcastEnabled(true);
            broadcastSender.SetDestAddress("255.255.255.255", BroadcastPort);
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

        public void StartListening()
        {
            if (isListening) return;
            broadcastListener = new PacketPeerUdp();
            broadcastListener.Bind(BroadcastPort);
            isListening = true;
        }

        public void StopListening()
        {
            if (!isListening) return;
            isListening = false;
            broadcastListener?.Close();
            broadcastListener = null;
        }

        public void Process(double delta)
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

        private void SendBroadcastPacket()
        {
            var data = new Dictionary
        {
            { "type", "room_info" },
            { "name", roomName },
            { "playerCount", roomPlayerCount },
            { "maxPlayers", roomMaxPlayers },
            { "gamePort", NetCore.m_Port } // 需要访问 NetCore 的端口常量
        };
            string json = Json.Stringify(data);
            broadcastSender.PutPacket(System.Text.Encoding.UTF8.GetBytes(json));
        }

        private void ProcessBroadcastData(byte[] bytes, string senderIP)
        {
            string json = System.Text.Encoding.UTF8.GetString(bytes);
            var data = Json.ParseString(json).AsGodotDictionary();
            if (data == null || (string)data["type"] != "room_info") return;

            string name = (string)data["name"];
            int playerCount = (int)(float)data["playerCount"];
            int maxPlayers = (int)(float)data["maxPlayers"];
            int port = (int)(float)data["gamePort"];

            OnRoomFound?.Invoke(name, senderIP, port, playerCount, maxPlayers);
        }
    }
}