using Godot;
using System.Collections.Generic;
using System.Xml.Linq;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Manager
{
    /// <summary>注：网络对象管理器，负责预制体管理、对象生成与销毁，主机调用生成方法，客户端被动同步。</summary>
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
                NetObjectRegistry.Instance.OnDestroyed += HandleDestroyed;
            }
            else
            {
                GD.PrintErr("[NetObjectManager] NetObjectRegistry 实例尚未就绪");
            }
            CatLog.Ok($"[NetObjectManager]：已完成初始化，载入资源数量[{m_PrefabDict.Count}]");
        }

        /// <summary>注：根据哈希值获取预制件，未找到则输出错误。</summary>
        public PackedScene GetPrefab(int hash)
        {
            if (!m_PrefabDict.TryGetValue(hash, out var result))
            {
                GD.PrintErr($"[NetObjectManager.GetPrefab]：未有找到 hash 对应的预制件-哈希值:{hash}");
                return null;
            }
            return result;
        }

        /// <summary>注：根据预制件名称获取预制件，未找到则返回 null。</summary>
        public PackedScene GetPrefab(string prefabName)
        {
            int hash = GetPrefabHash(prefabName);
            if (hash == default) return null;

            return m_PrefabDict[hash];
        }

        /// <summary>注：判断是否包含指定 NetID 的网络对象。</summary>
        public bool ContainsNetObject(NetID netID)
        {
            if (_netObjectInstances.TryGetValue(netID, out Node node))
            {
                return true;
            }

            return false;
        }

        /// <summary>注：获取指定 NetID 的网络对象，未找到则返回 null。</summary>
        public Node GetNetObject(NetID netID)
        {
            if (_netObjectInstances.TryGetValue(netID, out Node node))
            {
                return node;
            }

            return null;
        }

        /// <summary>注：判断是否包含指定哈希值的预制件。</summary>
        public bool ContainsPrefab(int hash)
        {
            if (m_PrefabDict.TryGetValue(hash, out var result)) return true;
            else
            {
                CatLog.Warn($"[NetObjectManager.ContainsPrefab]：未有对应的预制件-哈希值:{hash}");
                return false;
            }
        }

        /// <summary>注：获取预制件哈希值，未找到对应预制件则输出错误并返回默认值。</summary>
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

        /// <summary>注：根据参数生成网络对象，参数无效则输出错误。</summary>
        public bool SpawnObject(Vector3 pos, Vector3 rot, int hash = default, Node node = null , NetObject netObject = null)
        {
            if (hash != default && node == null && netObject == null)
            {
                if (ContainsPrefab(hash) == false) return false;

                HandleSpawned(NetObjectRegistry.Instance.RegisterObject(hash, pos, rot));
                return true;
            }
            else if (node != null && hash == default && netObject == null)
            {
                if (!IsInstanceValid(node)) return false;
                int nodehash = GetPrefabHash(node.Name);
                if (nodehash == default) return false;

                HandleSpawned(NetObjectRegistry.Instance.RegisterObject(nodehash, pos, rot), node);
                return true;
            }
            else if (netObject != null && hash == default && node == null)
            {
                HandleSpawned(NetObjectRegistry.Instance.RegisterObject(netObject.PrefabHash, pos, rot, netObject));
                return true;
            }
            else
            {
               
                GD.PrintErr("[NetObjectManager.SpawnObject]：无效参数");
                return false;
            }
        }

        /// <summary>注：处理网络对象生成，已存在则跳过，否则实例化并设置相关属性。</summary>
        private void HandleSpawned(NetID m_id, Node node = null)
        {
            if (_netObjectInstances.ContainsKey(m_id))
            {
                CatLog.Warn($"[{NetCore.Instance.LocalPeerID}][NetObjectManager.HandleSpawned] NetID {m_id} 的实例已存在，跳过创建");
                CatLog.Warn($"[{NetCore.Instance.LocalPeerID}][NetObjectManager.HandleSpawned] 重复对象 {GetNetObject(m_id).Name}");
                return;
            }

            NetObject netobj = NetObjectRegistry.Instance.GetNetObject(m_id);
            if (netobj == null)
            {
                CatLog.Warn($"[{NetCore.Instance.LocalPeerID}][NetObjectManager.HandleSpawned] NetID {m_id} 的 NetObject 是空的");
                return;
            }

            var currentScene = WorldManager.Instance.GetCurrentScene();
            if (currentScene == null) return;

            if (netobj.sceneHash == default)
            {
                if (currentScene.m_sceneData.m_sceneType != SceneData.SceneType.GameScene) return;
                netobj.sceneHash = currentScene.m_sceneData.m_sceneHash;
            }

            if (node == null)
            {
                if (!m_PrefabDict.TryGetValue(netobj.PrefabHash, out PackedScene scene))
                {
                    CatLog.Err($"[NetObjectManager.HandleSpawned]：使用对象 hash 未获得-PackedScene,哈希：{netobj.PrefabHash}");
                    return;
                }

                Node instance = scene.Instantiate();
                if (instance is not Node3D node3D) return;

                node3D.Name = $"{node3D.Name}-{netobj.Id}";

                var sync = node3D.GetNodeOrNull<NetSyncBase>("NetSyncBase");
                if (sync == null) return;
                sync.m_NetObj = netobj;
                _netObjectInstances[m_id] = instance;
                node3D.Position = netobj.Position;
                node3D.Rotation = netobj.Rotation;
                currentScene.AddChild(node3D);
            }
            else
            {
                if (node is not Node3D node3D) return;

                node3D.Name = $"{node3D.Name}-{netobj.Id}";

                var sync = node3D.GetNodeOrNull<NetSyncBase>("NetSyncBase");
                if (sync == null) return;
                sync.m_NetObj = netobj;
                _netObjectInstances[m_id] = node;
                node3D.Position = netobj.Position;
                node3D.Rotation = netobj.Rotation;
                currentScene.AddChild(node3D);
            }

        }

        /// <summary>注：网络对象注销时，从管理器中移除并清理场景实例。</summary>
        private void HandleDestroyed(NetID id)
        {
            if (_netObjectInstances.TryGetValue(id, out Node node))
            {
                _netObjectInstances.Remove(id);

                // 安全清理：如果节点还有效且未销毁，就移除它
                if (GodotObject.IsInstanceValid(node) && node.IsInsideTree())
                {
                    node.QueueFree();
                }
            }
        }

        /// <summary>注：打印网络实例的调试信息，包括数量及对象类型。</summary>
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