using Godot;
using 途畔归所.Dll.Manager;

namespace 途畔归所.Dll.NetWork
{
	public partial class NetTransformSync : Node
	{
		[Export] private float _syncInterval = 0.05f;      // 改为 20Hz，更流畅
		[Export] private float _smoothLerpSpeed = 15.0f;    // 插值速度
		[Export] private Node3D _node3D;

		private float _timer;
		private NetSyncBase _sync;

		// 目标位置/旋转（仅非 Owner 使用）
		private Vector3 _targetPosition;
		private Vector3 _targetRotation;
		private bool _hasTarget;

		public override void _Ready()
		{
			var node = GetParent();
			if (_node3D == null)
			{
				if (node == null || node is not Node3D node3)
				{
					GD.PrintErr("[NetTransformSync._Ready]：未获取到 Node3D");
					return;
				}
				_node3D = node3;
			}

			var nodearr = node.GetChildren();
			foreach (var comp in nodearr)
			{
				if (comp is NetSyncBase sync)
				{
					_sync = sync;
					break;
				}
			}

			if (_sync == null)
			{
				GD.PrintErr("[NetTransformSync._Ready] 未找到 NetSyncBase");
				SetProcess(false);
				return;
			}

			if (!NetObjectManager.Instance.ContainsNetObject(_sync.m_NetObj.Id))
			{
				GD.PrintErr("[NetTransformSync._Ready] NetSyncBase 未在网络实例中注册");
				SetProcess(false);
				return;
			}
		}

		public override void _Process(double delta)
		{
			if (_sync == null) return;

			// 远程对象：平滑插值到目标位置
			if (!_sync.IsOwner)
			{
				if (!_hasTarget) return;

				_node3D.GlobalPosition = _node3D.GlobalPosition.Lerp(_targetPosition, _smoothLerpSpeed * (float)delta);
				_node3D.GlobalRotation = _node3D.GlobalRotation.Lerp(_targetRotation, _smoothLerpSpeed * (float)delta);
				return;
			}

			// ---- 以下为 Owner 逻辑 ----
			_timer += (float)delta;
			if (_timer < _syncInterval) return;
			_timer = 0f;

			if (NetCore.Instance.IsHost)
			{
				Rpc(nameof(Rpc_NetTransformSync),
					_sync.m_NetObj.Id.UserID,
					_sync.m_NetObj.Id.ID,
					_node3D.GlobalPosition,
					_node3D.GlobalRotation);
			}
			// 客户端暂时不发送（单向同步）
		}

		[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
		private void Rpc_NetTransformSync(long userId, uint objId, Vector3 pos, Vector3 rot)
		{
			NetID netID = new(userId, objId);
			var node3d = NetObjectManager.Instance.GetNetObject(netID) as Node3D;
			if (node3d == null) return;

			// 不直接设置，改为存储目标，由 _Process 插值
			// 通过同一个节点上的 NetTransformSync 来接收
			// 注意：这里必须找到远程对象自己的 NetTransformSync
			var sync = node3d.GetNodeOrNull<NetTransformSync>("NetTransformSync");
			if (sync == null) return;

			sync._targetPosition = pos;
			sync._targetRotation = rot;
			sync._hasTarget = true;
		}
	}
}
