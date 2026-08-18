using Godot;
using System;
using System.Collections.Generic;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.NetWork;

[GlobalClass]
/// <summary>注：网络同步组件，管理RPC注册与调用分发、对象身份注册</summary>
public partial class NetSyncBase : Node
{
	private Node3D _node3D;
	private int _nodeHash;
	private SceneBase _scene;

	public NetObject NetObj => NetObjectRegistry.Instance.GetNetObject(NetID);
	public NetID NetID { get; set; }
	public bool IsOwner => _scene != null && _scene.OwnerPeerID == NetCore.Instance.LocalPeerID;
	public long OwnedPeer => _scene.OwnerPeerID;
	public long LocalPeer => NetCore.Instance.LocalPeerID;

	public byte[] CustomData { get => GetCustomData(); set => SetCustomData(value); }

	/// <summary>是否正在等待数据响应（传输中锁，防止重复点击）</summary>
	public bool IsDataSyncing { get; private set; }

	private Dictionary<string, Action<long, Variant>> RpcMap { get; set; } = [];



	public override void _EnterTree()
	{
		if (ValidateDeps()) CatUtils.StopAndExit(GetParent());
		
	}

	public override void _Ready() => RegisterManual();

	public override void _ExitTree()
	{
		NetObjectManager.Instance.RemoveObject(NetID);

	}

	private bool ValidateDeps()
	{
		
		if (GetParent() is not Node3D node3D)
		{
			CatLog.Err("[NetSyncBase.ValidateDeps]：挂载的组件对象，不是Node3D类型，已删除");
			return true;
		}

		if (WorldManager.Instance.GetCurrentScene() is not SceneBase scene)
		{
			CatLog.Err("[NetSyncBase.ValidateDeps]：NetSyncBase 没有存在 与 游戏场景内 已删除");
			return true;
		}

		_node3D = node3D;
		_nodeHash = CatUtils.GetStableHashCode(_node3D.Name);
		_scene = scene;

		return false;
	}

	private void RegisterManual()
	{

		if (!IsOwner && NetID.IsNone)
		{
			CatLog.Ok($"[NetSyncBase]：销毁前检测 NetID:{NetID}，{_node3D.Name}");
			CatUtils.StopAndExit(_node3D);
			return;
		}

		if (_scene.SceneData.IsNewScene && NetID.IsNone)
		{
			NetObjectManager.Instance.SpawnObject(_nodeHash, _node3D.GlobalPosition, _node3D.GlobalRotation);
			CatLog.Ok($"[NetSyncBase]：新场景预置物品，已注册 NetID:{_node3D.Name}");
		}

		if (NetID.IsNone)
		{
			CatLog.Ok($"[NetSyncBase]：销毁前检测 NetID:{NetID}，{_node3D.Name}");
			CatUtils.StopAndExit(_node3D);
		}
	}

	/// <summary>请求最新权威数据，数据就绪后执行回调</summary>
	public void RequestCustomData(Action onDataReady = null)
	{
		if (IsDataSyncing)
		{
			CatLog.Warn($"[NetSyncBase] NetID:{NetID} 同步中，重复请求已忽略");
			return;
		}

		IsDataSyncing = true;

		// 订阅一次数据更新，触发后自动解绑
		void DataReadyHandler()
		{
			onDataReady?.Invoke();
			NetObj.OnDataChanged -= DataReadyHandler;
			IsDataSyncing = false;
		}

		NetObj.OnDataChanged += DataReadyHandler;

		// 超时保护：3秒无响应自动解锁
		var timer = GetTree().CreateTimer(3.0);
		timer.Timeout += () =>
		{
			if (IsDataSyncing)
			{
				IsDataSyncing = false;
				NetObj.OnDataChanged -= DataReadyHandler;
				CatLog.Warn($"[NetSyncBase] NetID:{NetID} 数据请求超时");
			}
		};

		NetObjectRegistry.Instance.SendRequestCustomData(NetID);
	}


	/// <summary>提交本地修改到权威端</summary>
	public void SubmitCustomData(Action onDataReady = null)
	{
		if (IsDataSyncing)
		{
			CatLog.Warn($"[NetSyncBase] Submit 被忽略，同步中。NetID:{NetID}");
			return;
		}

		if (NetCore.Instance.IsHost)
		{
			CatLog.Debug($"[NetSyncBase] Submit: 主机直接提交。NetID:{NetID}");
			onDataReady?.Invoke();
			return;
		}

		IsDataSyncing = true;
		CatLog.Debug($"[NetSyncBase] Submit 发起。NetID:{NetID} 版本:{NetObj?.DataRevision}");

		void DataReadyHandler()
		{
			CatLog.Ok($"[NetSyncBase] Submit 收到确认。NetID:{NetID} 新版本:{NetObj?.DataRevision}");
			onDataReady?.Invoke();
			NetObj.OnDataChanged -= DataReadyHandler;
			IsDataSyncing = false;
		}

		NetObj.OnDataChanged += DataReadyHandler;

		var timer = GetTree().CreateTimer(3.0);
		timer.Timeout += () =>
		{
			if (IsDataSyncing)
			{
				IsDataSyncing = false;
				NetObj.OnDataChanged -= DataReadyHandler;
				CatLog.Warn($"[NetSyncBase] Submit 超时！NetID:{NetID} 版本:{NetObj?.DataRevision}");
			}
		};

		NetObjectRegistry.Instance.SendSubmitCustomData(NetID, NetObj.DataRevision, NetObj.CustomData);
	}

	private byte[] GetCustomData()
	{
		if (NetObj == null) return null;
		return NetObj.CustomData;
	}

	private void SetCustomData(byte[] data)
	{
		if (NetObj == null) return;
		NetObj.CustomData = data;
	}


	#region RPC 注册与调用

	// ─── RPC 注册（注册到自己的 RpcMap，而不是全局） ────────

	public void RegisterRpc(string name, Action action)
	{
		if (!CheckBeforeRegisterRpc(name)) return;
		RpcMap[name] = RpcGateway.Instance.MakeRpcHandler(action);
	}

	public void RegisterRpc(string name, Action<long> action)
	{
		if (!CheckBeforeRegisterRpc(name)) return;
		RpcMap[name] = RpcGateway.Instance.MakeRpcHandler(action);
	}

	public void RegisterRpc<[MustBeVariant] T1>(string name, Action<long, T1> action)
	{
		if (!CheckBeforeRegisterRpc(name)) return;
		RpcMap[name] = RpcGateway.Instance.MakeRpcHandler(action);
	}

	public void RegisterRpc<[MustBeVariant] T1, [MustBeVariant] T2>(string name, Action<long, T1, T2> action)
	{
		if (!CheckBeforeRegisterRpc(name)) return;
		RpcMap[name] = RpcGateway.Instance.MakeRpcHandler(action);
	}

	public void RegisterRpc<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3>(string name, Action<long, T1, T2, T3> action)
	{
		if (!CheckBeforeRegisterRpc(name)) return;
		RpcMap[name] = RpcGateway.Instance.MakeRpcHandler(action);
	}

	public void RegisterRpc<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4>(string name, Action<long, T1, T2, T3, T4> action)
	{
		if (!CheckBeforeRegisterRpc(name)) return;
		RpcMap[name] = RpcGateway.Instance.MakeRpcHandler(action);
	}

	public void RegisterRpc<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4, [MustBeVariant] T5>(string name, Action<long, T1, T2, T3, T4, T5> action)
	{
		if (!CheckBeforeRegisterRpc(name)) return;
		RpcMap[name] = RpcGateway.Instance.MakeRpcHandler(action);
	}


	/// <summary>注：公共注册前置检查，返回true代表可以继续注册</summary>
	private bool CheckBeforeRegisterRpc(string name)
	{
		if (RpcMap.ContainsKey(name))
		{
			CatLog.Warn($"[NetSyncBase.RegisterRpc]：重名RPC 方法{name}");
			return false;
		}
		return true;
	}


	/// <summary>注：可靠发送 RPC 给主机</summary>
	public void SendRpcToHost(string name, params Variant[] args) => RpcGateway.Instance.SendRpcToHost(NetID, name, args);

	/// <summary>注：不可靠发送 RPC 给主机</summary>
	public void SendFastRpcToHost(string name, params Variant[] args) => RpcGateway.Instance.SendFastRpcToHost(NetID, name, args);

	/// <summary>注：可靠发送 RPC 给指定对等端</summary>
	public void SendRpcToPeer(string name, long targetPeerId, params Variant[] args) => RpcGateway.Instance.SendRpcToPeer(NetID, name, targetPeerId, args);

	/// <summary>注：不可靠发送 RPC 给指定对等端</summary>
	public void SendFastRpcToPeer(string name, long targetPeerId, params Variant[] args) => RpcGateway.Instance.SendFastRpcToPeer(NetID, name, targetPeerId, args);

	/// <summary>注：可靠广播 RPC 给所有客户端</summary>
	public void SendRpcBroadcast(string name, params Variant[] args) => RpcGateway.Instance.SendRpcBroadcast(NetID, name,args);

	/// <summary>注：不可靠广播 RPC 给所有客户端</summary>
	public void SendFastRpcBroadcast(string name, params Variant[] args) => RpcGateway.Instance.SendFastRpcBroadcast(NetID, name, args);

	public void DispatchRpc(string name, Variant variant)
	{
		long senderId = Multiplayer.GetRemoteSenderId();
		if (RpcMap.TryGetValue(name, out var action))
		{
			action?.Invoke(senderId, variant);
		}
		else
		{
			CatLog.Warn($"[NetSyncBase] 未注册的 RPC：{name}，目标：{NetID}");
		}
	}

	#endregion
}
