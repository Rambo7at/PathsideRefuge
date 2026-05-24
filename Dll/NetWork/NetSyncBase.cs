using Godot;
using System;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using static System.Formats.Asn1.AsnWriter;

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

		public override void _Ready()
		{




		}

		public override void _Process(double delta)
		{

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
					CatLog.Ok("[NetSyncBase._Ready]：客户端场景，已销毁");
					node3D.QueueFree(); 
					return;
				}
				m_NetObj.sceneHash = m_NetObj.sceneHash == sceneBase.m_sceneData.m_sceneHash ? m_NetObj.sceneHash : sceneBase.m_sceneData.m_sceneHash;
			}

			sceneBase.OnFlushNetState += () => OnFlushNetState?.Invoke();
			IsInit = true;
		}

	}
}
