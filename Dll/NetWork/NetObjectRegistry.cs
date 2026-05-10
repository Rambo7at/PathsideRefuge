using Godot;
using System;
using System.Collections.Generic;
using 途畔归所.Dll.NetWork;

/// <summary>
/// 分布式对象登记处。负责 NetObj 的创建/销毁、事件触发，通过原生 RPC 广播给所有客户端。
/// （同步调度暂时关闭，仅保留基础的对象生成与销毁广播）
/// </summary>
public partial class NetObjectRegistry : Node
{
	private static NetObjectRegistry _instance;
	public static NetObjectRegistry Instance { get => _instance ??= new(); set => _instance ??= value; }

	// ── 核心数据 ──
	private readonly Dictionary<NetID, NetObject> _objects = [];
	private uint _nextObjID = 1;

	// 同步调度暂时关闭，以下字段注释掉
	// private readonly Dictionary<long, PeerSyncState> _peerStates = [];
	// private float _syncTimer;
	// private const float SyncInterval = 0.05f;

	// ── 对象 RPC 映射 ──
	private readonly Dictionary<NetID, NetSyncBase> _syncBaseMap = [];

	// ── 事件 ──
	public event Action<NetID> OnSpawned;
	public event Action<NetID> OnDestroyed;
	// public event Action<NetID> OnDataChanged;   // 同步未启用，暂不触发

	public override void _Ready()
	{
		Instance = this;
		// 不再需要自定义 RPC 注册，原生 RPC 特性自动处理
	}

	// 每帧更新暂时禁用（同步未启用）
	// public override void _Process(double delta) { ... }

	#region 公开接口：创建/销毁/查询

	/// <summary>
	/// 创建网络对象，分配 ID 并广播创建消息（仅主机执行广播）。
	/// 本地立即触发 OnSpawned 事件，以便 NetObjManager 实例化节点。
	/// </summary>
	public NetObject Spawn(int hash, Vector3 pos, Quaternion rot, long owner = -1)
	{
		if (owner == -1) owner = NetCore.Instance.LocalPeerID;

		NetID id = new(NetCore.Instance.LocalPeerID, _nextObjID++);
		NetObject netobj = new(id, pos, rot, hash, owner);
		_objects[id] = netobj;

		if (NetCore.Instance.IsHost)
		{
			// 通过原生 RPC 广播给所有客户端
			byte[] payload = SerializeCreate(netobj);
			Rpc("BroadcastCreate", payload);
		}

		// 本地触发（主机和客户端各自触发自己的实例化）
		OnSpawned?.Invoke(id);
		return netobj;
	}

	/// <summary>
	/// 销毁网络对象，本地移除并广播销毁消息（仅主机执行广播）。
	/// </summary>
	public void Destroy(NetObject netobj)
	{
		if (netobj == null || !_objects.ContainsKey(netobj.Id)) return;
		_objects.Remove(netobj.Id);

		if (NetCore.Instance.IsHost)
		{
			byte[] payload = SerializeNetID(netobj.Id);
			Rpc("BroadcastDestroy", payload);
		}
	}

	/// <summary>根据 NetID 获取对应的网络对象</summary>
	public NetObject GetNetObj(NetID id) =>
		_objects.TryGetValue(id, out var netobj) ? netobj : null;

	/// <summary>判断本地客户端是否为对象所有者</summary>
	public bool IsOwner(NetID id) =>
		_objects.TryGetValue(id, out var netobj) && netobj.IsOwner(NetCore.Instance.LocalPeerID);

	#endregion

	#region NetSyncBase 映射管理（不变）

	public void RegisterNetSyncBase(NetSyncBase syncBase) =>
		_syncBaseMap[syncBase.NetID] = syncBase;

	public void UnregisterNetSyncBase(NetSyncBase syncBase) =>
		_syncBaseMap.Remove(syncBase.NetID);

	#endregion

	#region 原生 RPC 方法（由主机广播，客户端执行，主机本地不执行 CallLocal = false）

	/// <summary>
	/// 广播创建对象消息给所有客户端。
	/// 此方法在客户端上被调用，负责在客户端本地注册对象并触发实例化。
	/// </summary>
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	private void BroadcastCreate(byte[] payload)
	{
		var (id, prefabHash, pos, rot, owner) = DeserializeCreate(payload);
		var netobj = new NetObject(id, pos, rot, prefabHash, owner);
		_objects[id] = netobj;
		OnSpawned?.Invoke(id);
	}

	/// <summary>
	/// 广播销毁对象消息给所有客户端。
	/// 此方法在客户端上被调用，负责在客户端本地移除对象并触发销毁事件。
	/// </summary>
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	private void BroadcastDestroy(byte[] payload)
	{
		var id = DeserializeNetID(payload);
		_objects.Remove(id);
		OnDestroyed?.Invoke(id);
	}

	#endregion

	//══════════════════════════════════════════════════
	//  序列化方法（全部保留，与原版完全一致）
	//══════════════════════════════════════════════════

	private byte[] SerializeCreate(NetObject netobj)
	{
		using var stream = new System.IO.MemoryStream();
		using var writer = new System.IO.BinaryWriter(stream);
		writer.Write(netobj.Id.UserID);
		writer.Write(netobj.Id.ID);
		writer.Write(netobj.PrefabHash);
		writer.Write(netobj.Position.X); writer.Write(netobj.Position.Y); writer.Write(netobj.Position.Z);
		writer.Write(netobj.Rotation.X); writer.Write(netobj.Rotation.Y); writer.Write(netobj.Rotation.Z); writer.Write(netobj.Rotation.W);
		writer.Write(netobj.OwnerPeerID);
		return stream.ToArray();
	}

	private (NetID id, int prefabHash, Vector3 pos, Quaternion rot, long owner) DeserializeCreate(byte[] data)
	{
		using var stream = new System.IO.MemoryStream(data);
		using var reader = new System.IO.BinaryReader(stream);
		var id = new NetID(reader.ReadInt64(), reader.ReadUInt32());
		int prefabHash = reader.ReadInt32();
		var pos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		var rot = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		long owner = reader.ReadInt64();
		return (id, prefabHash, pos, rot, owner);
	}

	private byte[] SerializeNetID(NetID id)
	{
		using var stream = new System.IO.MemoryStream();
		using var writer = new System.IO.BinaryWriter(stream);
		writer.Write(id.UserID);
		writer.Write(id.ID);
		return stream.ToArray();
	}

	private NetID DeserializeNetID(byte[] data)
	{
		using var stream = new System.IO.MemoryStream(data);
		using var reader = new System.IO.BinaryReader(stream);
		return new NetID(reader.ReadInt64(), reader.ReadUInt32());
	}

	// 同步数据序列化（暂时保留但未使用，后续可恢复）
	// private byte[] SerializeSyncData(List<NetObject> list) { ... }
	// private List<...> DeserializeSyncData(byte[] data) { ... }

	// ─── 连接回调（已从 NetCore 中解耦，此处不再自动调用） ───
	// 如果需要补发对象，可在主机收到客户端的某个 RPC 后调用 SyncAllObjectsToPeer

	// private void SyncAllObjectsToPeer(long peerId) { ... }

	// 清理所有对象数据
	public void ClearAll()
	{
		_objects.Clear();
		_syncBaseMap.Clear();
		_nextObjID = 1;
		GD.Print("[NetObjectRegistry] 已清空所有对象数据");
	}
}
