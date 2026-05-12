using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using 途畔归所.Dll.Manager;

namespace 途畔归所.Dll.NetWork
{
	public partial class NetObjectRegistry : Node
	{
		private static NetObjectRegistry _instance;
		public static NetObjectRegistry Instance { get => _instance ??= new(); set => _instance ??= value; }

		private readonly Dictionary<NetID, NetObject> _netObjects = new();
		private uint _nextObjID = 1;

		public event Action<NetID,Node> OnSpawned;
		public event Action<NetID> OnDestroyed;

		public override void _Ready()
		{
			Instance = this;
			Multiplayer.PeerConnected += OnPeerConnected;
		}


		public NetID GetNetID() => new(NetCore.Instance.LocalPeerID, _nextObjID++);

		public NetID RegisterObject(int hash, Vector3 pos, Vector3 rot)
		{
			int peer = NetCore.Instance.LocalPeerID;
			NetID id = GetNetID();
			NetObject netobj = new(id, pos, rot, hash, peer);
			_netObjects[id] = netobj;

			if (NetCore.Instance.IsHost)
			{
				Rpc(nameof(Rpc_HostSyncRegister), id.UserID,id.ID, hash, pos, rot);
				return id;
			}
			else
			{
				Rpc(nameof(Rpc_ReportToServer), id.UserID, id.ID, hash,pos, rot);
				return id;
			}

		}


		[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
		private void Rpc_HostSyncRegister(long userId, uint objId, int hash, Vector3 pos, Vector3 rot)
		{
			NetID id = new(userId, objId);

			if (!_netObjects.ContainsKey(id))
			{
				var netobj = new NetObject(id, pos, rot, hash, userId);
				_netObjects[id] = netobj;
			}

			OnSpawned?.Invoke(id, null);


			//GD.PrintErr($"[NetObjectRegistry]：[目标对象是：{userId}]---[哈希值：{hash}]---[_objects字典内已存在跳过]");
		}


		[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
		private void Rpc_ReportToServer(long userId, uint objId, int hash, Vector3 pos, Vector3 rot)
		{
			// 只有服务器执行
			if (!NetCore.Instance.IsHost) return;

			NetID id = new(userId, objId);

			if (_netObjects.ContainsKey(id))
			{
				GD.PrintErr($"[NetObjectRegistry] 重复上报的 NetID: {id}");
				return;
			}

			// 服务器权威登记
			var netobj = new NetObject(id, pos, rot, hash, userId); // owner 为上报者
			_netObjects[id] = netobj;

			// 广播给所有其他客户端（包括上报者也会收到，但 NetObjectManager 可以幂等处理）
			Rpc(nameof(Rpc_HostSyncRegister), id.UserID, id.ID, hash, pos, rot);

			OnSpawned?.Invoke(id, null);
		}


		private void OnPeerConnected(long id)
		{
			if (!NetCore.Instance.IsHost) return;

			GD.Print($"[NetObjectRegistry] 新客户端加入 (ID:{id})，开始向其同步已有对象...");
			foreach (var kvp in _netObjects)
			{
				NetObject netObj = kvp.Value;
				// 定向发给新客户端
				RpcId(id, nameof(Rpc_SyncToPeer),netObj.OwnerPeerID,kvp.Key.ID,netObj.PrefabHash,netObj.Position,netObj.Rotation);
			}
			GD.Print($"[NetObjectRegistry] 已向客户端 {id} 补发 {_netObjects.Count} 个对象");
		}

		[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
		private void Rpc_SyncToPeer(long userId, uint objId, int hash, Vector3 pos, Vector3 rot)
		{
			NetID id = new(userId, objId);
			if (_netObjects.ContainsKey(id)) return;   // 可能已经由客户自己上报过了

			var netobj = new NetObject(id, pos, rot, hash, userId);
			_netObjects[id] = netobj;
			OnSpawned?.Invoke(id,null);   // 触发实例化
		}


		public NetObject GetNetObject(NetID id) => _netObjects.TryGetValue(id, out var netobj) ? netobj : null;

		private void DebugPrintNetObjects()
		{

			GD.PrintErr($"[NetObjectRegistry]：[目标对象是：{NetCore.Instance.LocalPeerID}]-[数量：{_netObjects.Count}]");

			foreach (var item in _netObjects)
			{
				GD.PrintErr($"[NetObjectRegistry]：[目标对象是：{NetCore.Instance.LocalPeerID}]-[对象：{item.Value.PrefabHash}]");

			}
		}
	}
}
