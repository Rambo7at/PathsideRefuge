using Godot;
using System.Collections.Generic;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Manager
{
    public partial class NetObjectManager : Node
    {


        private static NetObjectManager _instance;
        public static NetObjectManager Instance { get => _instance ??= new(); set => _instance ??= value; }



        public Dictionary<int, PackedScene> m_PrefabDict = [];
        private Dictionary<NetID, Node> _instances = [];

        public override void _Ready()
        {
            Instance = this;

            foreach (var item in m_PrefabDict) GD.Print($"[NetObjectManager.m_PrefabDict]：这里有：{item.Value.ResourceName}");

            // ✅ 改用 NetObjectRegistry 的事件
            NetObjectRegistry.Instance.OnSpawned += HandleSpawned;
            NetObjectRegistry.Instance.OnDestroyed += HandleDestroyed;

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
            int hash = CatUtils.GetStableHashCode(prefabName);
            if (!m_PrefabDict.TryGetValue(hash, out var result))
            {
                GD.PrintErr($"[NetObjectManager.GetPrefab]：未有对应的预制件-名称:{prefabName}");
                return null;
            }
            return result;
        }

        public bool IsContainsPrefab(string prefabName)
        {
            int hash = CatUtils.GetStableHashCode(prefabName);
            if (!m_PrefabDict.TryGetValue(hash, out var result))
            {
                GD.PrintErr($"[NetObjectManager.GetPrefab]：未有对应的预制件-名称:{prefabName}");
                return false;
            }
            return true;
        }





        private void HandleSpawned(NetID id)
        {
            NetObject netobj = NetObjectRegistry.Instance.GetNetObj(id);
            if (netobj == null) return;

            if (!m_PrefabDict.TryGetValue(netobj.PrefabHash, out PackedScene scene)) return;

            Node instance = scene.Instantiate();
            if (instance is Node3D node3D)
            {
                node3D.Position = netobj.Position;
                node3D.Quaternion = netobj.Rotation;
            }

            // 先绑定 NetSyncBase（此时节点未入树，_Ready 不会执行）
            NetSyncBase syncBase = instance.GetNode<NetSyncBase>("NetSyncBase") ?? FindNetSyncBase(instance);
            syncBase?.Setup(netobj);

            // 先加入字典，再入树（保证入树后 _Ready 中如果需要查找某些依赖这个实例的管理器，不会为空）
            _instances[id] = instance;

            // 最后才入树，触发 _Ready
            GetTree().Root.AddChild(instance);
        }

        private void HandleDestroyed(NetID id)
        {
            if (_instances.TryGetValue(id, out Node node))
            {
                node.QueueFree();
                _instances.Remove(id);
            }
        }

        // 备用查找（如果节点名不是 NetSyncBase）
        private NetSyncBase FindNetSyncBase(Node root)
        {
            foreach (Node child in root.GetChildren())
            {
                if (child is NetSyncBase sb) return sb;
                var found = FindNetSyncBase(child);
                if (found != null) return found;
            }
            return null;
        }
    }
}