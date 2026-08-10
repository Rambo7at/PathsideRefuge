using Godot;
using System.Text.Json;
using 途畔归所.Dll.Interface;

namespace 途畔归所.Dll.NetWork;

/// <summary>注：网络对象数据载体，包含位置、旋转、自定义数据及唯一标识</summary>
public partial class NetObject : Resource, ISerializable
{
	[Export] public int PrefabHash { get; set; }
	[Export] public Vector3 Position { get; set; }
	[Export] public Vector3 Rotation { get; set; }
	[Export] public Variant m_customData { get; set; }

	public NetID netId { get; set; }

	public NetObject() { }

	public NetObject(NetID id, int prefabHash, Vector3 position, Vector3 rotation)
	{
		netId = id;
		PrefabHash = prefabHash;
		Position = position;
		Rotation = rotation;
	}

	private struct NetObjectDto    // 优化标记，之后可能会发现 netId 内的数据不需要进行到反序列化，本身都是需要重建的
	{
		public long UserID { get; set; }
		public uint ID { get; set; }
		public int SceneHash { get; set; }
		public int PrefabHash { get; set; }
		public float PosX { get; set; }
		public float PosY { get; set; }
		public float PosZ { get; set; }
		public float RotX { get; set; }
		public float RotY { get; set; }
		public float RotZ { get; set; }
		public string CustomData { get; set; }
	}

	public byte[] Serialize()
	{
		var dto = new NetObjectDto
		{
			UserID = netId.OwnerPeerID,
			ID = netId.LocalSeqId,
			SceneHash = netId.SceneHash,
			PrefabHash = PrefabHash,
			PosX = Position.X,
			PosY = Position.Y,
			PosZ = Position.Z,
			RotX = Rotation.X,
			RotY = Rotation.Y,
			RotZ = Rotation.Z,
			CustomData = Json.Stringify(m_customData)
		};

		return JsonSerializer.SerializeToUtf8Bytes(dto);
	}

	public void Deserialize(byte[] data)
	{
		var dto = JsonSerializer.Deserialize<NetObjectDto>(data);
		netId = new NetID(dto.UserID, dto.ID, dto.SceneHash);
		PrefabHash = dto.PrefabHash;
		Position = new Vector3(dto.PosX, dto.PosY, dto.PosZ);
		Rotation = new Vector3(dto.RotX, dto.RotY, dto.RotZ);
		m_customData = Json.ParseString(dto.CustomData);
	}
}
