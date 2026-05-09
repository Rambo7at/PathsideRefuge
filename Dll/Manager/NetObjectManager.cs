using Godot;
using System.Collections.Generic;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Manager
{
    public partial class NetObjectManager : Node
    {
        private Dictionary<NetID, Node> _instances = new();

        private static NetObjectManager _Instance;
        public static NetObjectManager Instance { get => _Instance; set => _Instance ??= value; }

        public Dictionary<int, PackedScene> m_PrefabDict = [];
        public override void _Ready()
        {
            GD.Print($"[NetObjManager]：查询到prefab数据一共:{m_PrefabDict.Count}");
            foreach (var item in m_PrefabDict)
            {
                GD.Print($"[NetObjManager]：这里有：{item.Value.ResourceName}");
            }

            NetCore.Instance.OnSpawned += HandleSpawned;
            NetCore.Instance.OnDestroyed += HandleDestroyed;

            GD.Print($"[NetObjManager]：已完成初始化");
        }


        



        private void HandleSpawned(NetID id)
        {
            NetObject netobj = NetCore.Instance.GetNetObj(id);
            string path = NetCore.Instance.GetPrefabPathByHash(netobj.PrefabHash); // 需在 NetCore 暴露方法
            PackedScene scene = GD.Load<PackedScene>(path);
            Node instance = scene.Instantiate();
            instance.GetNode<NetSyncBase>("NetSyncBase").Setup(netobj);
            GetTree().Root.AddChild(instance); // 或者添加到合适的地方
            _instances[id] = instance;
        }

        private void HandleDestroyed(NetID id)
        {
            if (_instances.TryGetValue(id, out Node node))
            {
                node.QueueFree();
                _instances.Remove(id);
            }
        }
    }
}
