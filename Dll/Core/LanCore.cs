using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Core;

public class LanCore
{
    private string _roomName;
    private const int _gamePort = 3043;
    private const int _port = 3044;

    private int _maxPlayer;
    private PacketPeerUdp m_packetPeerUdp;

    private bool IsHost = false;

    private float _Time;

    /// <summary>注：搜到的所有局域网房间（key = 房间 IP）</summary>
    private readonly Dictionary<string, RoomInfo> _rooms = new();

    /// <summary>注：房间信息</summary>
    public class RoomInfo
    {
        public string IP;
        public string RoomName;
        public int MaxPlayers;
        public double LastSeenTime;
    }

    public LanCore(string roomName, int maxPlayer)
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

            // 清理超过 5 秒没收到广播的过期房间
            CleanExpiredRooms(5.0);
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
        Godot.Collections.Dictionary<string, Variant> data = [];

        data.Add("room_Name", _roomName);
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


    private void ProcessBroadcastData(byte[] bytes, string senderIP)
    {
        if (bytes == null || bytes.Length < 4) return;

        Variant data = GD.BytesToVar(bytes);
        if (data.Obj == null) return;

        try
        {
            var dict = data.AsGodotDictionary();
            if (dict == null) return;
            if (!dict.ContainsKey("room_Name")) return;

            string roomName = dict["room_Name"].AsString();
            int maxPlayers = dict["max_Player"].AsInt32();

            // 更新或添加房间（同一个 IP 会覆盖旧信息）
            if (_rooms.TryGetValue(senderIP, out var room))
            {
                room.RoomName = roomName;
                room.MaxPlayers = maxPlayers;
                room.LastSeenTime = Time.GetTicksMsec() / 1000.0;
            }
            else
            {
                _rooms[senderIP] = new RoomInfo
                {
                    IP = senderIP,
                    RoomName = roomName,
                    MaxPlayers = maxPlayers,
                    LastSeenTime = Time.GetTicksMsec() / 1000.0
                };
            }
        }
        catch (Exception e)
        {
            CatLog.Warn($"[LanCore] 无效广播包: {e.Message}");
        }
    }


    /// <summary>注：清理过期房间（超过 timeout 秒没收到广播的）</summary>
    private void CleanExpiredRooms(double timeoutSec)
    {
        double now = Time.GetTicksMsec() / 1000.0;
        List<string> expiredKeys = new();

        foreach (var kv in _rooms)
        {
            if (now - kv.Value.LastSeenTime > timeoutSec)
            {
                expiredKeys.Add(kv.Key);
            }
        }

        foreach (var key in expiredKeys)
        {
            _rooms.Remove(key);
        }
    }


    /// <summary>注：获取当前搜到的所有房间列表</summary>
    public List<RoomInfo> GetRooms()
    {
        return _rooms.Values.ToList();
    }
}

