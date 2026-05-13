using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;

namespace 途畔归所.Dll.NetWork
{
	[GlobalClass]
	public partial class NetTransformSync : Node
	{
		[Export] private float _syncInterval = 0.05f;
		[Export] private float _smoothLerpSpeed = 15.0f;
		[Export] private Node3D _node3D;
		[Export] private Node3D _rotMesh;

		private float _timer;
		private NetSyncBase _sync;

		private Vector3 _targetPosition;
		private Vector3 _targetRotation;
		private bool _hasTarget;

		public override void _Ready()
		{
			var parent = GetParent();
			if (_node3D == null)
			{
				if (parent is not Node3D node3) return;
				_node3D = node3;
			}

			_sync = parent.GetNodeOrNull<NetSyncBase>("NetSyncBase");
			if (_sync == null || !NetObjectManager.Instance.ContainsNetObject(_sync.m_NetObj.Id))
			{
				SetProcess(false);
				return;
			}

			// 自动查找 rotMesh
			if (_rotMesh == null)
				_rotMesh = _node3D.GetNodeOrNull<Node3D>("m_PlayerModel");
		}

		public override void _Process(double delta)
		{
			if (_sync == null) return;

			if (!_sync.IsOwner)
			{
				// 远程对象插值
				if (!_hasTarget) return;

				_node3D.GlobalPosition = _node3D.GlobalPosition.Lerp(_targetPosition, _smoothLerpSpeed * (float)delta);
				if (_rotMesh != null)
					_rotMesh.GlobalRotation = _rotMesh.GlobalRotation.Lerp(_targetRotation, _smoothLerpSpeed * (float)delta);
				else
					_node3D.GlobalRotation = _node3D.GlobalRotation.Lerp(_targetRotation, _smoothLerpSpeed * (float)delta);
				return;
			}

			// 以下为 Owner 逻辑
			_timer += (float)delta;
			if (_timer < _syncInterval) return;
			_timer = 0f;

			Vector3 pos = _node3D.GlobalPosition;
			Vector3 rot = _rotMesh != null ? _rotMesh.GlobalRotation : _node3D.GlobalRotation;

			if (NetCore.Instance.IsHost)
			{
				// 主机直接广播给所有客户端（排除自己）
				Rpc(nameof(Rpc_NetTransformSync),
					_sync.m_NetObj.Id.UserID,
					_sync.m_NetObj.Id.ID,
					pos, rot);
			}
			else
			{
				// 客户端上报给主机
				RpcId(NetCore.ServerID, nameof(Rpc_ClientTransformReport),
					_sync.m_NetObj.Id.UserID,
					_sync.m_NetObj.Id.ID,
					pos, rot);
			}
		}

		// 客户端上报给主机
		[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
		private void Rpc_ClientTransformReport(long userId, uint objId, Vector3 pos, Vector3 rot)
		{
			if (!NetCore.Instance.IsHost) return;

			NetID netID = new(userId, objId);
			var node = NetObjectManager.Instance.GetNetObject(netID) as Node3D;
			if (node == null) return;

			// ★ 平滑更新：交给节点自己的 NetTransformSync 插值处理
			var sync = node.GetNodeOrNull<NetTransformSync>("NetTransformSync");
			if (sync != null)
			{
				sync._targetPosition = pos;
				sync._targetRotation = rot;
				sync._hasTarget = true;
			}

			// ★ 转发给除发送者外的其他所有客户端（包括其他客户端，不包括发送者自己）
			long senderId = Multiplayer.GetRemoteSenderId();
			foreach (long peerId in Multiplayer.GetPeers())
			{
				if (peerId != senderId && peerId != NetCore.ServerID)
				{
					RpcId(peerId, nameof(Rpc_NetTransformSync), userId, objId, pos, rot);
				}
			}
		}

		// 所有客户端接收变换（原 RPC 保持不变）
		[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
		private void Rpc_NetTransformSync(long userId, uint objId, Vector3 pos, Vector3 rot)
		{
			NetID netID = new(userId, objId);
			var node3d = NetObjectManager.Instance.GetNetObject(netID) as Node3D;
			if (node3d == null) return;

			var sync = node3d.GetNodeOrNull<NetTransformSync>("NetTransformSync");
			if (sync == null) return;

			sync._targetPosition = pos;
			sync._targetRotation = rot;
			sync._hasTarget = true;
		}
	}
}
