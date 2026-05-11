using Godot;
using System.Collections.Generic;
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

		private Dictionary<NetID, Node> _instances = [];

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
			GD.Print($"[NetObjectManager]：{m_PrefabDict.Count}");
			GD.Print($"[NetObjectManager]：已完成初始化");
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
			int hash = IsContainsPrefab(prefabName);
			if (hash == default) return null;

			return m_PrefabDict[hash];
		}

		public bool IsContainsPrefab(int hash)
		{
			if (m_PrefabDict.TryGetValue(hash, out var result)) return true;
            else return false;
		}

        public int IsContainsPrefab(string prefabName)
		{
			int hash = CatUtils.GetStableHashCode(prefabName);
			if (IsContainsPrefab(hash) == false)
			{
				GD.PrintErr($"[NetObjectManager.GetPrefab]：未有对应的预制件-名称:{prefabName}");
				return default;
			}
			return hash;
		}



		public void SpawnObject(Vector3 pos, Quaternion rot, int hash = default, Node node = null )
		{
			if (node != null)
			{
                if (IsContainsPrefab(hash) == false) return;
                var ID = NetObjectRegistry.Instance.RegistryObject(hash, pos, rot);
                HandleSpawned(ID);
            }
			if (hash != default)
			{
                if (node == null || !IsInstanceValid(node)) return;
                int nodehash = IsContainsPrefab(node.Name);
                var ID = NetObjectRegistry.Instance.RegistryObject(hash, pos, rot);
                HandleSpawned(node, ID, pos, rot);
            }
			else
			{
				GD.Print();
		
			}
		
		}




        public void SpawnObject(int hash, Vector3 pos, Quaternion rot)
		{
			if (!m_PrefabDict.ContainsKey(hash)) return;
			var ID = NetObjectRegistry.Instance.RegistryObject(hash, pos, rot);
			HandleSpawned(ID);
		}

		public void SpawnObject(Node node, Vector3 pos, Quaternion rot)
		{
			if (node == null || !IsInstanceValid(node)) return;
			int hash = IsContainsPrefab(node.Name);
			if (hash == default) return;

			var ID = NetObjectRegistry.Instance.RegistryObject(hash, pos, rot);
			HandleSpawned(node, ID, pos, rot);
		}

		private void HandleSpawned(NetID id)
		{
			if (_instances.ContainsKey(id))
			{
				GD.Print($"[NetObjectManager] NetID {id} 的实例已存在，跳过创建");
				return;
			}

			NetObject netobj = NetObjectRegistry.Instance.GetNetObj(id);
			if (netobj == null) return;

			if (!m_PrefabDict.TryGetValue(netobj.PrefabHash, out PackedScene scene)) return;
			Node instance = scene.Instantiate();
			if (instance is not Node3D node3D) return;


            var sync = node3D.GetNodeOrNull<NetSyncBase>("NetSyncBase");
            if (sync != null)
            {
                sync.m_NetID = id;
                sync.m_OwnerPeerID = netobj.OwnerPeerID;
                sync.EmitNetworkReady();
            }

            node3D.Position = netobj.Position;
			node3D.Quaternion = netobj.Rotation;

			_instances[id] = instance;
			GetTree().Root.AddChild(instance);
		}

		private void HandleSpawned(Node node, NetID id, Vector3 pos, Quaternion rot)
		{
            if (_instances.ContainsKey(id))
            {
                GD.Print($"[NetObjectManager] NetID {id} 的实例已存在，跳过创建");
                return;
            }

            if (node == null) return;
			if (!IsInstanceValid(node)) return;
			if (node is not Node3D node3D) return;

            node3D.Position = pos;
			node3D.Quaternion = rot;

			NetObject netobj = NetObjectRegistry.Instance.GetNetObj(id);
			_instances[id] = node;

			GetTree().Root.AddChild(node3D);
		}





		private void TEst(NetID id)
		{

			int count = _instances.Count;

			GD.PrintErr($"[NetObjectManager]：[目标对象是：{NetCore.Instance.LocalPeerID}]-[数量：{count}]");

			foreach (var item in _instances)
			{
				GD.PrintErr($"[NetObjectManager]：[目标对象是：{NetCore.Instance.LocalPeerID}]-[对象：{item.Value.GetType().Name}]");

			}
		}
	}
}
