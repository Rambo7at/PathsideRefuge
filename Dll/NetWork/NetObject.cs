using Godot;
using System;
using System.Text.Json;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.NetWork;

/// <summary>注：网络对象数据载体，包含位置、旋转、自定义数据及唯一标识</summary>
public partial class NetObject : Resource, ISerializable
{
    [Export] public int PrefabHash { get; set; }
    [Export] public Vector3 Position { get; set; }
    [Export] public Vector3 Rotation { get; set; }

    [Export] private byte[] _customData;


    public byte[] CustomData
    {
        get => _customData;
        set
        {
            if (_customData == value) return;
            _customData = value;
            DataRevision++;
            OnDataChanged?.Invoke();
        }
    }




    public uint DataRevision { get; private set; }

    public NetID netId { get; set; }



    public event Action OnDataChanged;

    public NetObject() { }

    public NetObject(NetID id, int prefabHash, Vector3 position, Vector3 rotation)
    {
        netId = id;
        PrefabHash = prefabHash;
        Position = position;
        Rotation = rotation;
    }


    public NetObject DeepCopy() => this.DuplicateDeep() as NetObject;




    /// <summary>应用权威数据（客户端同步专用，直接设置数据并标记版本）</summary>
    public void ApplyAuthoritativeData(uint revision, byte[] data)
    {
        _customData = data;
        DataRevision = revision;
        OnDataChanged?.Invoke();
    }

    /// <summary>通知数据已确认（触发 OnCustomDataUpdated 事件）</summary>
    public void NotifyDataConfirmed()
    {
        OnDataChanged?.Invoke();
    }



    public struct NetObjectDto
    {
        public long PeerID { get; set; }
        public uint LocalSeqId { get; set; }
        public int SceneHash { get; set; }
        public int PrefabHash { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float RotX { get; set; }
        public float RotY { get; set; }
        public float RotZ { get; set; }
    }

    /// <summary>序列化NetObject基础实体信息（位置、预制体、NetID等）</summary>
    public byte[] Serialize()
    {
        var dto = new NetObjectDto
        {
            PeerID = netId.PeerID,
            LocalSeqId = netId.LocalSeqId,
            SceneHash = netId.SceneHash,
            PrefabHash = PrefabHash,
            PosX = Position.X,
            PosY = Position.Y,
            PosZ = Position.Z,
            RotX = Rotation.X,
            RotY = Rotation.Y,
            RotZ = Rotation.Z,
        };
        return JsonSerializer.SerializeToUtf8Bytes(dto);
    }

    /// <summary>反序列化NetObject基础实体信息</summary>
    public void Deserialize(byte[] data)
    {
        var dto = JsonSerializer.Deserialize<NetObjectDto>(data);
        netId = new NetID(dto.PeerID, dto.LocalSeqId, dto.SceneHash);
        PrefabHash = dto.PrefabHash;
        Position = new Vector3(dto.PosX, dto.PosY, dto.PosZ);
        Rotation = new Vector3(dto.RotX, dto.RotY, dto.RotZ);
    }

}