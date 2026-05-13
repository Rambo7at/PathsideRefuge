using Godot;
using System.Collections.Generic;
using System.Xml.Linq;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Manager
{

	/// <summary>
	/// 网络对象管理器：所有网络对象生成的唯一入口。
	/// 负责维护预制体字典、在主机端发起对象创建并广播，客户端接收广播后执行本地实例化，
	/// 同时统一管理所有网络节点的销毁。
	/// 仅主机可调用生成方法，客户端通过 RPC 被动同步。
	/// </summary>
	public partial class NetObjectManager : Node
	{
		private static NetObjectManager _instance;
		public static NetObjectManager Instance { get => _instance ??= new(); set => _instance ??= value; }



		public Dictionary<int, PackedScene> m_PrefabDict = [];

		private Dictionary<NetID, Node> _netObjectInstances = [];

		public override void _Ready()
		{
			Instance = this;

		

			if (NetObjectRegistry.Instance != null)
			{
				NetObjectRegistry.Instance.OnSpawned += HandleSpawned;
			}
			else
			{
				GD.PrintErr("[NetObjectManager] NetObjectRegistry 实例尚未就绪");
			}
			CatLog.Ok($"[NetObjectManager]：已完成初始化，载入资源数量[{m_PrefabDict.Count}]");
		}

		public PackedScene GetPrefab(int hash)
		{
			if (!m_PrefabDict.TryGetValue(hash, out var result))
			{
				GD.PrintErr($"[NetObjectManager.GetPrefab]：未有找到 hash 对应的预制件-哈希值:{hash}");
				return null;
			}
			return result;
		}

		public PackedScene GetPrefab(string prefabName)
		{
			int hash = GetPrefabHash(prefabName);
			if (hash == default) return null;

			return m_PrefabDict[hash];
		}

		public bool ContainsNetObject(NetID netID)
		{
			if (_netObjectInstances.TryGetValue(netID,out Node node))
			{
				return true;
			}
		
		     return false;
		}


		public Node GetNetObject(NetID netID)
		{
            if (_netObjectInstances.TryGetValue(netID, out Node node))
            {
                return node;
            }

            return null;
        }




        public bool ContainsPrefab(int hash)
		{
			if (m_PrefabDict.TryGetValue(hash, out var result)) return true;
			else return false;
		}

		public int GetPrefabHash(string prefabName)
		{
			int hash = CatUtils.GetStableHashCode(prefabName);
			if (ContainsPrefab(hash) == false)
			{
				GD.PrintErr($"[NetObjectManager.GetPrefab]：未有对应的预制件-名称:{prefabName}");
				return default;
			}
			return hash;
		}

		public void SpawnObject(Vector3 pos, Vector3 rot, int hash = default, Node node = null )
		{
			if (hash != default && node == null) 
			{
				if (ContainsPrefab(hash) == false) return;

				var ID = NetObjectRegistry.Instance.RegisterObject(hash, pos, rot);
				HandleSpawned(ID);
			}
			else if (node != null && hash == default)
			{
				if (!IsInstanceValid(node)) return;
				int nodehash = GetPrefabHash(node.Name);
				if (nodehash == default) return;

				var ID = NetObjectRegistry.Instance.RegisterObject(nodehash, pos, rot);
				HandleSpawned(ID, node);
			}
			else
			{
				GD.PrintErr("[NetObjectManager.SpawnObject]：无效参数");

			}
		}

		private void HandleSpawned(NetID m_id, Node node = null)
		{
			if (_netObjectInstances.ContainsKey(m_id))
			{
				GD.Print($"[NetObjectManager.HandleSpawned] NetID {m_id} 的实例已存在，跳过创建");
				return;
			}

			NetObject netobj = NetObjectRegistry.Instance.GetNetObject(m_id);
			if (netobj == null) return;

			if (node == null)
			{

				if (!m_PrefabDict.TryGetValue(netobj.PrefabHash, out PackedScene scene)) return;
				Node instance = scene.Instantiate();
				if (instance is not Node3D node3D) return;


				var sync = node3D.GetNodeOrNull<NetSyncBase>("NetSyncBase");
				if (sync == null) return;
				sync.m_NetObj = netobj;

                _netObjectInstances[m_id] = instance;
                GetTree().Root.AddChild(instance);

                node3D.GlobalPosition = netobj.Position;
                node3D.GlobalRotation = netobj.Rotation;
            }
			else
			{

				if (!IsInstanceValid(node)) return;
				if (node is not Node3D node3D) return;

				var sync = node3D.GetNodeOrNull<NetSyncBase>("NetSyncBase");
				if (sync == null) return;
				sync.m_NetObj = netobj;

                _netObjectInstances[m_id] = node;
                GetTree().Root.AddChild(node3D);

                node3D.GlobalPosition = netobj.Position;
                node3D.GlobalRotation = netobj.Rotation;

            }

		}

		private void DebugPrintNetInstances(NetID id)
		{

			int count = _netObjectInstances.Count;

			GD.PrintErr($"[NetObjectManager]：[目标对象是：{NetCore.Instance.LocalPeerID}]-[数量：{count}]");

			foreach (var item in _netObjectInstances)
			{
				GD.PrintErr($"[NetObjectManager]：[目标对象是：{NetCore.Instance.LocalPeerID}]-[对象：{item.Value.GetType().Name}]");

			}
		}
	}
}
