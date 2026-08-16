using Godot;
using System;
using System.Collections.Generic;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Manager;

/// <summary>注：网络对象注册表，管理网络对象注册、同步及相关事件。</summary>
public partial class NetObjectRegistry : Node
{
	private static NetObjectRegistry _instance;
	public static NetObjectRegistry Instance { get => _instance ??= new(); set => _instance ??= value; }

	private readonly Dictionary<NetID, NetObject> _netObjects = [];

	/// <summary>注：场景哈希 → 该场景所有 NetID 列表，用于快速检索场景对象</summary>
	private readonly Dictionary<int, List<NetID>> _sceneNetObjectsList = [];

	private uint _nextObjID = 1;

	public event Action<NetID, Node> OnSpawned;
	public event Action<NetID> OnDestroyed;

	public override void _Ready()
	{
		Instance = this;
		CatLog.Ok("[NetObjectRegistry] 已初始化");
	}

	/// <summary>注：获取一个新的网络对象 ID（包含当前场景哈希）。</summary>
	private NetID GetNetID()
	{
		int sceneHash = WorldManager.Instance.CurrentSceneHash;
		return new NetID(NetCore.Instance.LocalPeerID, _nextObjID++, sceneHash);
	}

	/// <summary>注：获取指定场景哈希的新网络对象 ID。</summary>
	private NetID GetNetID(int sceneHash) => new(NetCore.Instance.LocalPeerID, _nextObjID++, sceneHash);

	/// <summary>注：注册并生成网络对象，同步信息给其他节点，返回 NetID。</summary>
	public NetID RegisterAndSpawn(int hash, Vector3 pos, Vector3 rot)
	{
		NetID id = GetNetID();
		NetObject netobj = new(id, hash, pos, rot);
		_netObjects[id] = netobj;

		if (!_sceneNetObjectsList.ContainsKey(id.SceneHash)) _sceneNetObjectsList[id.SceneHash] = [];
		_sceneNetObjectsList[id.SceneHash].Add(id);

		SyncRegister(id.PeerID, id.LocalSeqId, id.SceneHash, netobj.PrefabHash, pos, rot);
		return id;
	}

	/// <summary>注：注册并生成网络对象（使用已有 NetObject），同步信息给其他节点，返回 NetID。</summary>
	public NetID RegisterAndSpawn(NetObject netobj, Vector3 pos, Vector3 rot)
	{
		NetID id = GetNetID();



		netobj.netId = id;
		_netObjects[id] = netobj;

		if (!_sceneNetObjectsList.ContainsKey(id.SceneHash)) _sceneNetObjectsList[id.SceneHash] = [];
		_sceneNetObjectsList[id.SceneHash].Add(id);

		SyncRegister(id.PeerID, id.LocalSeqId, id.SceneHash, netobj.PrefabHash, pos, rot);
		return id;
	}


	public void RegisterAndSpawn(NetID netid, NetObject netobj)
	{
		if (_netObjects.TryGetValue(netid, out var _)) return;

		_netObjects[netid] = netobj;
		OnSpawned?.Invoke(netid, null);
		SyncRegister(netid.PeerID, netid.LocalSeqId, netid.SceneHash, netobj.PrefabHash, netobj.Position, netobj.Rotation);

		return;
	}




	/// <summary>注：同步注册信息到其他节点，主机走 Rpc_HostSyncRegister，客户端走 Rpc_ReportToServer。</summary>
	private void SyncRegister(long peer, uint seqId, int sceneHash, int prefabHash, Vector3 pos, Vector3 rot)
	{
		if (NetCore.Instance.IsHost)
		{
			Rpc(nameof(Rpc_HostSyncRegister), peer, seqId, sceneHash, prefabHash, pos, rot);
		}
		else
		{
			Rpc(nameof(Rpc_ReportToServer), peer, seqId, sceneHash, prefabHash, pos, rot);
		}
	}

	/// <summary>注：批量注册网络对象（用于场景数据恢复），触发 OnSpawned 事件。</summary>
	public void RegistryNetObjects(Godot.Collections.Array<NetObject> netObjects)
	{
		foreach (var item in netObjects)
		{
			if (_netObjects.ContainsKey(item.netId)) continue;
			_netObjects[item.netId] = item;

			if (!_sceneNetObjectsList.ContainsKey(item.netId.SceneHash)) _sceneNetObjectsList[item.netId.SceneHash] = [];
			_sceneNetObjectsList[item.netId.SceneHash].Add(item.netId);

			OnSpawned?.Invoke(item.netId, null);
		}
	}

	/// <summary>注：空重载，预留扩展。</summary>
	public bool LoadNetObjects(int sceneHash)
	{
		if (!_sceneNetObjectsList.TryGetValue(sceneHash, out var netids)) return false;

		foreach (var id in netids)
		{
			OnSpawned?.Invoke(id, null);
		}

		return true;
	}

	/// <summary>注：客户端向服务器请求场景存档数据。</summary>
	public void RequestSceneData(int sceneHash) => RpcId(NetCore.ServerID, nameof(Rpc_RequestSceneData), sceneHash);


	#region 同步

	/// <summary>注：服务器处理场景存档请求，有存档则下发，无存档则通知客户端按预设生成。</summary>
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void Rpc_RequestSceneData(int sceneHash)
	{
		if (NetCore.Instance.IsClient) return;
		long sendPeer = Multiplayer.GetRemoteSenderId();

		// 搜寻场景数据
		if (_sceneNetObjectsList.TryGetValue(sceneHash, out var netIDs))
		{
			foreach (var netid in netIDs)
			{
				if (!_netObjects.TryGetValue(netid, out var netobj)) continue;
				RpcId(sendPeer, nameof(Rpc_SendNetObject), netid.PeerID, netid.LocalSeqId, netid.SceneHash, netobj.PrefabHash, netobj.Position, netobj.Rotation);
			}
			RpcId(sendPeer, nameof(Rpc_SceneDataReady), false);
			return;
		}

		// 从世界管理 加载场景存档数据
		if (WorldManager.Instance.LoadSceneData(sceneHash) is SceneData sceneData)
		{
			foreach (var netObj in sceneData.NetObjectList)
			{
				RpcId(sendPeer, nameof(Rpc_SendNetObject), netObj.netId.PeerID, netObj.netId.LocalSeqId, netObj.netId.SceneHash, netObj.PrefabHash, netObj.Position, netObj.Rotation);
			}
			RpcId(sendPeer, nameof(Rpc_SceneDataReady), false);
			return;
		}

		RpcId(sendPeer, nameof(Rpc_SceneDataReady), true);
	}

	/// <summary>注：服务器下发单个 NetObject 注册信息，客户端收到后注册并生成对象。</summary>
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	private void Rpc_SendNetObject(long peer, uint seqId, int sceneHash, int prefabHash, Vector3 pos, Vector3 rot)
	{
		if (NetCore.Instance.IsHost) return;
		NetID id = new(peer, seqId, sceneHash);
		NetObject netobj = new(id, prefabHash, pos, rot);
		RegisterAndSpawn(id, netobj);
	}

	/// <summary>注：服务器通知客户端没有场景存档，客户端按场景预设自行生成。</summary>
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	private void Rpc_SceneDataReady(bool IsNewScene)
	{
		if (NetCore.Instance.IsHost) return;

		WorldManager.Instance.CurrentScene.OnSceneDataReady(IsNewScene);
	}

	/// <summary>注：主机同步注册网络对象信息，并触发对象生成事件。</summary>
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	private void Rpc_HostSyncRegister(long ownerPeer, uint seqId, int sceneHash, int prefabHash, Vector3 pos, Vector3 rot)
	{
		if (NetCore.Instance.IsHost) return;

		NetID netId = new(ownerPeer, seqId, sceneHash);

		if (_netObjects.ContainsKey(netId)) return;

		var netobj = new NetObject(netId, prefabHash, pos, rot);

		_netObjects[netId] = netobj;

		OnSpawned?.Invoke(netId, null);
	}

	/// <summary>注：向服务器报告网络对象信息，服务器登记并广播给其他客户端，触发对象生成事件。</summary>
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void Rpc_ReportToServer(long ownerPeer, uint seqId, int sceneHash, int prefabHash, Vector3 pos, Vector3 rot)
	{
		if (NetCore.Instance.IsClient) return;
		long senderId = Multiplayer.GetRemoteSenderId();

		NetID netId = new(ownerPeer, seqId, sceneHash);
		var netobj = new NetObject(netId, prefabHash, pos, rot);

		if (!_netObjects.ContainsKey(netId)) _netObjects[netId] = netobj;

		foreach (long peerId in Multiplayer.GetPeers())
		{
			if (peerId != senderId && peerId != NetCore.ServerID)
			{
				RpcId(peerId, nameof(Rpc_HostSyncRegister), netId.PeerID, netId.LocalSeqId, netId.SceneHash, netobj.PrefabHash, pos, rot);
			}
		}

		OnSpawned?.Invoke(netId, null);
	}

	/// <summary>注：通知所有客户端销毁指定网络对象。</summary>
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void Rpc_DestroyNetObject(long ownerPeer, uint seqId, int sceneHash)
	{
		if (NetCore.Instance.IsHost) return;

		NetID id = new(ownerPeer, seqId, sceneHash);

		if (!_netObjects.ContainsKey(id))
		{
			CatLog.Debug($"[NetObjectRegistry] 对象 {id} 已被销毁或不存在");
			return;
		}

		RemoveNet(id);
	}
	#endregion

	#region 自定义数据传输

	/// <summary>注：客户端请求拉取指定 NetObject 的最新自定义数据。</summary>
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void Rpc_RequestCustomData(long peerID, uint localSeqId, int sceneHash)
	{
		if (NetCore.Instance.IsClient) return;
		long sendPeer = Multiplayer.GetRemoteSenderId();

		NetID netID = new(peerID, localSeqId, sceneHash);

		if (_netObjects.TryGetValue(netID, out var netObj))
		{
			RpcId(sendPeer, nameof(Rpc_ReceiveCustomData), peerID, localSeqId, sceneHash, netObj.DataRevision, netObj.CustomData);
			CatLog.Ok($"[Registry] 服务器发出数据信息");
		}

		// TODO: 未找到目标 NetID 时的处理逻辑
	}

	/// <summary>注：服务器下发自定义数据，客户端接收并应用。</summary>
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	private void Rpc_ReceiveCustomData(long peerID, uint localSeqId, int sceneHash, uint revision, byte[] data)
	{
		NetID netID = new(peerID, localSeqId, sceneHash);
		if (!_netObjects.TryGetValue(netID, out var netObj)) return;
		netObj.ApplyAuthoritativeData(revision, data);
		CatLog.Ok($"[Registry] 收到版本数据，   。NetID:{netID} 收到:{revision} 本地:{netObj.DataRevision}");
	}

	/// <summary>注：客户端提交修改后的自定义数据给服务器。</summary>
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void Rpc_SubmitCustomData(long peerID, uint localSeqId, int sceneHash, uint clientRevision, byte[] data)
	{
		if (NetCore.Instance.IsClient) return;
		NetID netID = new(peerID, localSeqId, sceneHash);
		long sendPeer = Multiplayer.GetRemoteSenderId();

		if (!_netObjects.TryGetValue(netID, out var netObj))
		{
			CatLog.Err($"[Registry] 没找到 对应的 NetID:{netID}");
			return;
		}

		if (clientRevision < netObj.DataRevision)
		{
			CatLog.Warn($"[Registry] 提交版本小于权威版本，拒绝覆盖。NetID:{netID} 客户端:{clientRevision} 权威:{netObj.DataRevision}");
			return;
		}

		netObj.CustomData = data;
		CatLog.Ok($"[Registry] 服务器收到版本数据。NetID:{netID} 新版本:{netObj.DataRevision}");
		RpcId(sendPeer, nameof(Rpc_AcknowledgeCustomData), peerID, localSeqId, sceneHash);
	}

	/// <summary>注：服务器确认收到客户端提交的数据，触发客户端 OnDataChanged 事件。</summary>
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	private void Rpc_AcknowledgeCustomData(long peerID, uint localSeqId, int sceneHash)
	{
		NetID netID = new(peerID, localSeqId, sceneHash);
		if (!_netObjects.TryGetValue(netID, out var netObj)) return;

		netObj.NotifyDataConfirmed();
	}

	/// <summary>注：客户端主动请求自定义数据。</summary>
	public void SendRequestCustomData(NetID netID) => RpcId(NetCore.ServerID, nameof(Rpc_RequestCustomData), netID.PeerID, netID.LocalSeqId, netID.SceneHash);

	/// <summary>注：客户端主动提交自定义数据。</summary>
	public void SendSubmitCustomData(NetID netID, uint revision, byte[] data) => RpcId(NetCore.ServerID, nameof(Rpc_SubmitCustomData), netID.PeerID, netID.LocalSeqId, netID.SceneHash, revision, data);

	#endregion

	/// <summary>注：主机广播销毁网络对象。</summary>
	public void BroadcastDestroyNetObject(NetObject netObject)
	{
		if (netObject == null) return;
		if (!NetCore.Instance.IsHost) return;

		NetID id = netObject.netId;
		Rpc(nameof(Rpc_DestroyNetObject), id.PeerID, id.LocalSeqId, id.SceneHash);
		CatLog.Net($"[NetObjectRegistry] 主机广播销毁对象：{id}");
	}

	/// <summary>注：从注册表中移除指定 NetID 的网络对象。</summary>
	public void RemoveNet(NetID ID)
	{
		if (!_netObjects.TryGetValue(ID, out var netObj)) return;
		_netObjects.Remove(ID);
		OnDestroyed?.Invoke(ID);
	}

	/// <summary>注：根据 NetID 获取对应的 NetObject。</summary>
	public NetObject GetNetObject(NetID id) => _netObjects.TryGetValue(id, out var netobj) ? netobj : null;

	/// <summary>注：获取指定场景的所有 NetObject 列表。</summary>
	public Godot.Collections.Array<NetObject> GetNetObjectsForScene(int sceneHash)
	{
		Godot.Collections.Array<NetObject> arr = [];

		foreach (var netobj in _netObjects)
		{
			if (netobj.Key.SceneHash != sceneHash) continue;
			arr.Add(netobj.Value);
		}

		return arr;
	}

	/// <summary>注：获取所有场景的 NetObject 字典（场景哈希 → NetObject 列表）。</summary>
	public Dictionary<NetID, NetObject> GetNetObjectsDict() => _netObjects;




	/// <summary>注：调试方法，打印所有 NetObject 信息。</summary>
	public void Debug_GetAllNetObjects()
	{
		foreach (var netObj in _netObjects)
		{
			CatLog.Warn($"[NetObjectRegistry]: NetID: {netObj.Key}, PrefabHash: {netObj.Value.PrefabHash}，ObjNetID：{netObj.Value.netId}");
			CatLog.Warn($"[NetObjectRegistry]: 数据检测 {netObj.Value.CustomData?.Length}");
		}
	}
}
