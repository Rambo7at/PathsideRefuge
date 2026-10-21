using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.NetWork;

[GlobalClass]
public partial class NetTransformSync : Node
{
	[Export] private float _syncInterval = 0.05f;
	[Export] private float _smoothLerpSpeed = 15.0f;

	private float _timer;
	private Node3D _parentNode;
	private NetSyncBase _netSyncBase;
	private NetObject _netobj => _netSyncBase.NetObj;

	private Vector3 CurPos { get => _parentNode.GlobalPosition; set => _parentNode.GlobalPosition = value; }
	private Vector3 CurRot { get => _parentNode.GlobalRotation; set => _parentNode.GlobalRotation = value; }
	private Vector3 TargetPos { get => _netobj.Position; set => _netobj.Position = value; }
	private Vector3 TargetRot { get => _netobj.Rotation; set => _netobj.Rotation = value; }
	
	private bool isPlayer;

	private bool IsOwner => isPlayer ? _netSyncBase?.NetID.PeerID == _netSyncBase?.LocalPeer : _netSyncBase.IsOwner;

	public override void _Ready()
	{
		if (GetParent() is not Node3D node3D)
		{
			CatUtils.StopAndExit(this);
			return;
		}

		_parentNode = node3D;

		if (_parentNode is Player) isPlayer = true;

		if (CatUtils.FindChildNode<NetSyncBase>(_parentNode) is not NetSyncBase Sync)
		{
			CatUtils.StopAndExit(this);
			return;
		}

		_netSyncBase = Sync;

		// 注册变换同步 RPC
		_netSyncBase.RegisterRpc<Vector3, Vector3>("RPC_TransformSync", RPC_TransformSync);
		_netSyncBase.RegisterRpc<Vector3, Vector3>("RPC_ClientTransformReport", RPC_ClientTransformReport);
	}

	public override void _Process(double delta)
	{
		if (!IsOwner) return;

		if (TargetPos == CurPos && TargetRot == CurRot) return;

		TargetPos = CurPos;
		TargetRot = CurRot;

		if (_netSyncBase.IsOwner)
		{
			_netSyncBase.SendFastRpcBroadcast("RPC_TransformSync", CurPos, CurRot);
		}
		else
		{
			_netSyncBase.SendFastRpcToPeer("RPC_ClientTransformReport", _netSyncBase.OwnedPeer, CurPos, CurRot);
		}
	}

	// 客户端上报给主机（由主机转发给其他客户端）
	private void RPC_ClientTransformReport(long senderId, Vector3 pos, Vector3 rot)
	{
		if (NetCore.Instance.IsClient) return;

		// 更新目标位置
		CurPos = pos;
		CurRot = rot;

		TargetPos = pos;
		TargetRot = rot;

		// 转发给其他客户端（排除发送者）
		foreach (long peerId in Multiplayer.GetPeers())
		{
			if (peerId != senderId && peerId != NetCore.ServerID)
			{
				_netSyncBase.SendFastRpcToPeer("RPC_TransformSync", peerId, pos, rot);
			}
		}
	}

	// 所有客户端接收变换
	private void RPC_TransformSync(long senderId, Vector3 pos, Vector3 rot)
	{
		if (NetCore.Instance.IsHost) return;


		CurPos = pos;
		CurRot = rot;

		TargetPos = pos;
		TargetRot = rot;
	}
}
