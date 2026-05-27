using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using static System.Collections.Specialized.BitVector32;

namespace 途畔归所.Dll.NetWork
{

	[GlobalClass]
	public partial class NetSyncBase : Node
	{
		private Node3D m_node3D;
		private int m_nodeHash;

		public NetObject m_NetObj { get; set; }
		public bool IsOwner => m_NetObj != null && m_NetObj.OwnerPeerID == NetCore.Instance.LocalPeerID;
		public bool IsInit = false;

		public System.Collections.Generic.Dictionary<string, Action<long, Variant>> RpcDict { get; set; } = [];
		public event Action OnFlushNetState;

		public override void _EnterTree()
		{
			var node = GetParent();
			if (node is not Node3D node3D)
			{
				CatLog.Err("[NetSyncBase._EnterTree]：挂载的组件对象，不是Node3D类型，已删除");
				node.QueueFree();
				return;
			}
			var scene = WorldManager.Instance.GetCurrentScene();
			EnsureNetObj(scene, node3D);
		}

		private void EnsureNetObj(SceneBase sceneBase, Node3D node3D)
		{
			m_node3D = node3D;
			m_nodeHash = CatUtils.GetStableHashCode(node3D.Name);

			if (m_NetObj == null)
			{
				if (sceneBase.m_sceneData.m_newScene == false)
				{
					CatUtils.StopAndExit(node3D);
					return;
				}
				if (NetCore.Instance.IsHost)
				{
					var ID = NetObjectRegistry.Instance.RegisterObject(m_nodeHash, m_node3D.GlobalPosition, m_node3D.GlobalRotation);
					var netobj = NetObjectRegistry.Instance.GetNetObject(ID);
					m_NetObj = netobj;
					CatLog.Ok("[NetSyncBase._Ready]：发现未注册组件，已提交注册");
				}
				else
				{
					CatLog.Net("[NetSyncBase._Ready]：对象是客户端，已销毁");
					node3D.QueueFree();
					return;
				}

				m_NetObj.sceneHash = m_NetObj.sceneHash == sceneBase.m_sceneData.m_sceneHash ? m_NetObj.sceneHash : sceneBase.m_sceneData.m_sceneHash;
			}

			sceneBase.OnFlushNetState += () => OnFlushNetState?.Invoke();
			IsInit = true;
		}


		public void RegisterRpc(string name, Action action) => RpcDict[name] = (id, _) => action();

		public void RegisterRpc(string name, Action<long> action) => RpcDict[name] = (id, _) => action(id);

		public void RegisterRpc<[MustBeVariant] T>(string name, Action<long, T> action) => RpcDict[name] = (id, value) => action(id, value.As<T>());

		public void RegisterRpc<[MustBeVariant] T>(string name, Action<T> action) => RpcDict[name] = (id, value) => action(value.As<T>());

		public void RegisterRpc<[MustBeVariant] T1, [MustBeVariant] T2>(string name, Action<long, T1, T2> action)
		{
			RpcDict[name] = (id, value) =>
			{
				var arr = value.As<Godot.Collections.Array>();

				if (arr == null || arr.Count < 2) return; 

				action(id, arr[0].As<T1>(), arr[1].As<T2>());
			};
		}

		public void CallRpc(string name, long Id = 1) => RpcId(Id, nameof(Rpc_Anypeer), name, default); 

		public void CallRpc(string name,  Variant value, long Id = 1) => RpcId(Id, nameof(Rpc_Anypeer), name, value);

		public void CallRpc(string name, Variant value1, Variant value2, long Id = 1) => CallRpc(name, new Godot.Collections.Array() { value1, value2 }, Id);

		public void CallAllRpc(string name) => Rpc(nameof(Rpc_Anypeer), name, default);

		public void CallAllRpc(string name, Variant value) => Rpc(nameof(Rpc_Anypeer), name, value);

		public void CallAllRpc(string name, Variant value1, Variant value2) => CallAllRpc(name, new Godot.Collections.Array { value1, value2 });


		[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
		public void Rpc_Anypeer(string name, Variant variant)
		{
			long senderId = Multiplayer.GetRemoteSenderId();
			if (RpcDict.TryGetValue(name, out var action))
				action?.Invoke(senderId, variant);
		}


	}
}
