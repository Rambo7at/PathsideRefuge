using Godot;
using System.Collections.Generic;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Base.SceneBase;

namespace 途畔归所.Dll.Manager;

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

    public void RegisterNetObject(int hash, PackedScene prefab)
    {
        if (m_PrefabDict.ContainsKey(hash))
        {
            GD.PrintErr($"[NetObjectManager.RegisterNetObject]：已存在相同哈希值的预制件，哈希值:{hash} + {prefab.ResourcePath}");
            return;
        }
        m_PrefabDict[hash] = prefab;
    }

    /// <summary>注：根据哈希值获取预制件，未找到则输出错误。</summary>
    public PackedScene GetPrefab(int hash)
    {
        if (!m_PrefabDict.TryGetValue(hash, out var result))
        {
            CatLog.Warn($"[NetObjectManager.GetPrefab]：未有找到 hash 对应的预制件-哈希值:{hash}");
            return null;
        }
        return result;
    }

    /// <summary>注：根据预制件名称获取预制件，未找到则返回 null。</summary>
    public PackedScene GetPrefab(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
        {
            CatLog.Warn($"[NetObjectManager.GetPrefab]：传入了一个空的字符");
            return null;
        }

        if (GetPrefab(CatUtils.GetStableHashCode(prefabName)) is not PackedScene prefab)
        {
            CatLog.Warn($"[NetObjectManager.GetPrefab]：未有找到对应的预制件-传入名称:{prefabName}");
            return null;

        }
        return prefab;
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

    /// <summary>注：通过预制体哈希生成网络对象</summary>
    public bool SpawnObject(int hash, Vector3 pos, Vector3 rot)
    {
        if (GetPrefab(hash) == null) return false;
        var netId = NetObjectRegistry.Instance.RegisterObject(hash, pos, rot);
        HandleSpawned(netId);
        return true;
    }

    /// <summary>注：通过已有节点生成网络对象（用于玩家等预置实例）</summary>
    public bool SpawnObject(Node3D node3D, Vector3 pos, Vector3 rot)
    {
        if (!IsInstanceValid(node3D)) return false;
        int hash = CatUtils.GetStableHashCode(node3D.Name);
        if (GetPrefab(hash) == null) return false;
        var netId = NetObjectRegistry.Instance.RegisterObject(hash, pos, rot);
        HandleSpawned(netId, node3D);
        return true;
    }

    /// <summary>注：通过已有 NetObject 数据生成网络对象（用于场景恢复）</summary>
    public bool SpawnObject(NetObject netObject, Vector3 pos, Vector3 rot)
    {
        if (netObject == null) return false;
        if (GetPrefab(netObject.PrefabHash) == null) return false;

        var netId = NetObjectRegistry.Instance.RegisterObject(netObject, pos, rot);


        HandleSpawned(netId);
        return true;
    }

    /// <summary>注：处理网络对象生成，实例化节点并添加到当前场景</summary>
    private void HandleSpawned(NetID netId, Node node = null)
    {

        // 2. 检查是否已存在
        if (_netObjectInstances.ContainsKey(netId))
        {
            CatLog.Net($"[NetObjectManager] NetID {netId} 已存在，跳过生成");
            return;
        }

        // 3. 获取 NetObject 数据
        NetObject netobj = NetObjectRegistry.Instance.GetNetObject(netId);
        if (netobj == null)
        {
            CatLog.Net($"[NetObjectManager] NetID {netId} 的 NetObject 为空");
            return;
        }

        // 4. 检查场景是否匹配
        var currentScene = WorldManager.Instance.GetCurrentScene();

        if (currentScene == null)
        {
            CatLog.Net("[NetObjectManager] 当前场景为空，无法生成对象");
            return;
        }

        if (netId.SceneHash != 0 && netId.SceneHash != currentScene.SceneData.SceneHash)
        {
            CatLog.Net($"[NetObjectManager] 忽略跨场景对象：{netId.SceneHash} != {currentScene.SceneData.SceneHash}");
            return;
        }

        // 5. 获取节点
        Node3D node3D = node as Node3D;
        if (node3D == null)
        {
            PackedScene prefab = GetPrefab(netobj.PrefabHash);
            if (prefab == null)
            {
                CatLog.Err($"[NetObjectManager] 预制体不存在，哈希：{netobj.PrefabHash}");
                return;
            }
            node3D = prefab.Instantiate<Node3D>();
        }

        // 6. 设置名称和位置
        node3D.Name = $"{node3D.Name}-{netId.SceneHash}-{netId.PeerID}-{netId.LocalSeqId}";
        node3D.Position = netobj.Position;
        node3D.Rotation = netobj.Rotation;



        if (CatUtils.FindChildNode<NetSyncBase>(node3D) is not NetSyncBase sync)
        {
            CatLog.Err($"[NetObjectManager] 没有对应的网络对象！ ");
            return;
        }

        sync.NetID = netId;
        _netObjectInstances[netId] = node3D;
        currentScene.AddChild(node3D);

        CatLog.Net($"[NetObjectManager] 生成网络对象：{node3D.Name}，NetID：{netId}");
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
