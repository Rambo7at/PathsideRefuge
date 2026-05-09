using Godot;
using System.Collections.Generic;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Manager
{
    public partial class NetObjectManager : Node
    {


        private static NetObjectManager _instance;
        public static NetObjectManager Instance { get => _instance; set => _instance ??= value; }



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
            GD.Print($"[NetObjectManager] HandleSpawned 被调用, NetID={id}");

            NetObject netobj = NetObjectRegistry.Instance.GetNetObj(id);
            if (netobj == null)
            {
                GD.PrintErr($"[NetObjectManager] 获取 NetObj 失败，NetID={id}");
                return;
            }

            if (!m_PrefabDict.TryGetValue(netobj.PrefabHash, out PackedScene scene))
            {
                GD.PrintErr($"[NetObjectManager] 找不到哈希 {netobj.PrefabHash} 对应的预制体");
                GD.Print("当前字典中的键:");
                foreach (var key in m_PrefabDict.Keys)
                    GD.Print($"  {key}");
                return;
            }

            Node instance = scene.Instantiate();

            // 1. 设置初始变换（使用局部坐标，因为此时不在树中）
            if (instance is Node3D node3D)
            {
                node3D.Position = netobj.Position;      // 因为父节点将设为 Root，所以 Position == GlobalPosition
                node3D.Quaternion = netobj.Rotation;
            }

            // 2. 查找并绑定 NetSyncBase（必须在进入树前完成，保证后续物理帧可用）
            NetSyncBase syncBase = instance.GetNode<NetSyncBase>("NetSyncBase")?? FindNetSyncBase(instance);
            syncBase?.Setup(netobj);

            // 3. 最后加入场景树（此时所有依赖已就绪）
            GetTree().Root.AddChild(instance);
            _instances[id] = instance;

            GD.Print($"[NetObjectManager] 成功实例化 NetID={id}");
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