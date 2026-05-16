using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using static System.Net.Mime.MediaTypeNames;

namespace 途畔归所.Dll.NetWork
{
	[GlobalClass]
	public partial class NetTransformSync : Node
	{
		[Export] private float _syncInterval = 0.05f;
		[Export] private float _smoothLerpSpeed = 15.0f;


		private float _timer;

        private Node3D _node3D;
        private NetSyncBase _sync;

		private Vector3 _curPos  { get=> _node3D.GlobalPosition; set => _node3D.GlobalPosition = value; }
        private Vector3 _curRot { get=> _node3D.GlobalRotation; set => _node3D.GlobalRotation = value; }

		private Vector3 _targetPos { get=> _sync.m_NetObj.Position ; set => _sync.m_NetObj.Position = value; }
		private Vector3 _targetRot { get => _sync.m_NetObj.Rotation; set => _sync.m_NetObj.Rotation = value; }


		public override void _Ready()
		{
			var ndoe = GetParent();

			if (ndoe is not Node3D node3)
			{
				CatLog.Err("[NetTransformSync._Ready]：挂载的组件对象，不是Node3D类型，已销毁");
				CatUtils.StopAndExit(this);
				return;
			}

			_node3D = node3;

			foreach (var comp in _node3D.GetChildren())
			{
				if (comp is NetSyncBase netSyncBase)
				{
					_sync = netSyncBase;
					break;
				}
			}

			if (_sync == null)
			{
                CatLog.Err("[NetTransformSync._Ready]：未有在挂载对象中找到 NetSyncBase 组件，已销毁");
                CatUtils.StopAndExit(this);
                return;
            }


        }

        public override void _Process(double delta)
        {
            if (!_sync.IsOwner) return;

            if (_targetPos == _curPos && _targetRot == _curRot) return;

            _targetPos = _curPos;
            _targetRot = _curRot;

            if (NetCore.Instance.IsHost)
            {
                Rpc(nameof(Rpc_NetTransformSync), _sync.m_NetObj.Id.UserID, _sync.m_NetObj.Id.ID, _curPos, _curRot);
            }
            else
            {
                RpcId(NetCore.ServerID, nameof(Rpc_ClientTransformReport), _sync.m_NetObj.Id.UserID, _sync.m_NetObj.Id.ID, _curPos, _curRot);
            }
        }

        // 客户端上报给主机
        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
		private void Rpc_ClientTransformReport(long userId, uint objId, Vector3 pos, Vector3 rot)
		{
			if (!NetCore.Instance.IsHost) return;

			NetID netID = new(userId, objId);
            if (NetObjectManager.Instance.GetNetObject(netID) is not Node3D node3d) return;

            var TransformSync = node3d.GetNodeOrNull<NetTransformSync>("NetTransformSync");
			if (TransformSync != null)
			{
                TransformSync._curPos = pos;
                TransformSync._curRot = rot;
                TransformSync._targetPos = pos;   
                TransformSync._targetRot = rot;
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

			if (NetObjectManager.Instance.GetNetObject(netID) is not Node3D node3d) return;

			var TransformSync = node3d.GetNodeOrNull<NetTransformSync>("NetTransformSync");
			if (TransformSync == null) return;

            TransformSync._curPos = pos;
            TransformSync._curRot = rot;
            TransformSync._targetPos = pos;  
            TransformSync._targetRot = rot;
        }
	}
}
