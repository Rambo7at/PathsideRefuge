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
		private NetObject _netobj;

		private Vector3 _curPos  { get=> _node3D.GlobalPosition; set => _node3D.GlobalPosition = value; }
		private Vector3 _curRot { get=> _node3D.GlobalRotation; set => _node3D.GlobalRotation = value; }

		private Vector3 _targetPos { get=> _netobj.Position ; set => _netobj.Position = value; }
		private Vector3 _targetRot { get => _netobj.Rotation; set => _netobj.Rotation = value; }


		public override void _Ready()
		{
			if (GetParent() is Node3D node3D) _node3D = node3D;

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

			if (_sync.m_NetObj == null) return;

			_netobj = _sync.m_NetObj;
		}

		public override void _Process(double delta)
		{
			if (!_sync.IsOwner) return;

			if (_targetPos == _curPos && _targetRot == _curRot) return;

			_targetPos = _curPos;
			_targetRot = _curRot;

			if (NetCore.Instance.IsHost)
			{
				Rpc(nameof(Rpc_NetTransformSync), _curPos, _curRot);
			}
			else
			{
				RpcId(NetCore.ServerID, nameof(Rpc_ClientTransformReport),_curPos, _curRot);
			}
		}

		// 客户端上报给主机
		[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
		private void Rpc_ClientTransformReport(Vector3 pos, Vector3 rot)
		{
			if (NetCore.Instance.IsClient) return;
			
			_curPos = pos;
			_curRot = rot;
			_targetPos = pos;
			_targetRot = rot;

			long senderId = Multiplayer.GetRemoteSenderId();
			foreach (long peerId in Multiplayer.GetPeers())
			{
				if (peerId != senderId && peerId != NetCore.ServerID)
				{
					RpcId(peerId, nameof(Rpc_NetTransformSync), _curPos, _curRot);
				}
			}
		}




		// 所有客户端接收变换（原 RPC 保持不变）
		[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
		private void Rpc_NetTransformSync(Vector3 pos, Vector3 rot)
		{
			if (NetCore.Instance.IsHost) return;

			_curPos = pos;
			_curRot = rot;
			_targetPos = pos;  
			_targetRot = rot;
		}
	}
}
