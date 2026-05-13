using Godot;
using Godot.Collections;
using System;
using System.Text;


namespace 途畔归所.Dll.Core
{
    public class LanCore
    {


        private string _roomName;
        private const int _gamePort = 3043;
        private const int _port = 3044;

        private int _maxPlayer;
        private PacketPeerUdp m_packetPeerUdp;

        private bool IsHost = false;

        private float _Time;

        public string LatestRoomIP { get; private set; }

        public LanCore(string roomName,int maxPlayer)
        {
            _roomName = roomName;
            _maxPlayer = maxPlayer;
            SendBroadcastPacket();
            IsHost = true;
        }

        public LanCore() => StartListening();



        public void Start(double delta)
        {
            if (IsHost)
            {
                _Time += (float)delta;
                if (_Time >= 2.0f)
                {
                    _Time = 0f;
                    StartBroadcast();         
                    SendBroadcastPacket();    
                }
            }
            else
            {

                while (m_packetPeerUdp.GetAvailablePacketCount() > 0)
                {
                    byte[] bytes = m_packetPeerUdp.GetPacket();
                    string ip = m_packetPeerUdp.GetPacketIP();
                    ProcessBroadcastData(bytes, ip);
                }


            }

        }




        public void StartBroadcast()
        {
            m_packetPeerUdp = new PacketPeerUdp();
            m_packetPeerUdp.SetBroadcastEnabled(true);
            m_packetPeerUdp.SetDestAddress("255.255.255.255", _port);
        }



        public void StopBroadcast()
        {
            m_packetPeerUdp.Close();
        }


        private void SendBroadcastPacket()
        {
            if (m_packetPeerUdp == null) return;
            Dictionary<string,Variant> data = [];

            data.Add("room_Name",_roomName);
            data.Add("max_Player", _maxPlayer);
            data.Add("game_Port", _gamePort);

            m_packetPeerUdp.PutPacket(GD.VarToBytes(data));
        }



        public void StartListening()
        {
            m_packetPeerUdp = new PacketPeerUdp();
            m_packetPeerUdp.Bind(_port);
        }

        public void StopListening()
        {
            m_packetPeerUdp?.Close();
            m_packetPeerUdp = null;
        }


        private string ProcessBroadcastData(byte[] bytes, string senderIP)
        {
            Variant data = GD.BytesToVar(bytes);
            if (data.Obj == null) return string.Empty;

            var dict = data.AsGodotDictionary();
            if (dict == null) return string.Empty;
            if (!dict.ContainsKey("room_Name")) return string.Empty;

            LatestRoomIP = senderIP;
            return senderIP;
        }







    }
}