using Godot;
using System.Text.Json;
using 途畔归所.Dll.Interface;

namespace 途畔归所.Dll.NetWork;

public partial class NetObject : Resource, ISerializable
{
	public NetID Id { get; set; }
	[Export] public int PrefabHash { get; set; }
	[Export] public long OwnerPeerID { get; set; }
	[Export] public int sceneHash { get; set; }
	[Export] public Vector3 Position { get; set; }
	[Export] public Vector3 Rotation { get; set; }
	[Export] public Variant m_customData { get; set; }

	public NetObject() { }
	public NetObject(NetID id, Vector3 position, Vector3 rotation, int prefabHash, long ownerPeerID)
	{
		Id = id;
		Position = position;
		Rotation = rotation;
		PrefabHash = prefabHash;
		OwnerPeerID = ownerPeerID;
	}

	public bool IsOwner(long localPeerID) => OwnerPeerID == localPeerID;
	
	private struct NetObjectDto
	{
		public long UserID { get; set; }
		public uint ID { get; set; }
		public int SceneHash { get; set; }
		public int PrefabHash { get; set; }
		public long OwnerPeerID { get; set; }
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
			UserID = Id.UserID,
			ID = Id.ID,
			SceneHash = sceneHash,
			PrefabHash = PrefabHash,
			OwnerPeerID = OwnerPeerID,
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
		Id = new NetID(dto.UserID, dto.ID, dto.SceneHash);
		PrefabHash = dto.PrefabHash;
		OwnerPeerID = dto.OwnerPeerID;
		sceneHash = dto.SceneHash;
		Position = new Vector3(dto.PosX, dto.PosY, dto.PosZ);
		Rotation = new Vector3(dto.RotX, dto.RotY, dto.RotZ);
		m_customData = Json.ParseString(dto.CustomData);
	}
}
