using Godot;
using Godot.Collections;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Comp;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Manager;

/// <summary>注：资源管理器</summary>
public class ResourceManager
{
    private static ResourceManager _instance;

    public static ResourceManager Instance => _instance ??= new ResourceManager();

    private Array<PackedScene> _resourceList = [];
    public Array<PackedScene> m_UIAssetList { get; private set; } = [];

    private ResourceManager() { }

    public void Init()
    {
        LoadDirectory("res://Prefab/");
        LoadDirectory("res://Scenes/");


        RegisterResource();

        CatLog.Ok("[ResourceManager] 已完成初始化");
    }

    /// <summary> 注: 注册网络对象管理器并处理资源列表 </summary>
    private void RegisterResource()
    {
        foreach (var prefab in _resourceList)
        {

            if (prefab.Instantiate() is not Node node) continue;

            if (string.IsNullOrEmpty(node.Name))
            {
                CatLog.Warn($"[ResourceManager.RegisterResource]：执行发现未有预制名的资源，文件地址: {prefab.ResourcePath}，已跳过");
                continue;
            }

            prefab.ResourceName = node.Name;
            int prefabHash = CatUtils.GetStableHashCode(node.Name);

            if (node is Control || node is CanvasLayer)
            {
                GUIManager.Instance.RegisterGUI(prefabHash,prefab);
                node.QueueFree();
                continue;
            }

            if (node is SceneBase)
            {
                WorldManager.Instance.RegisterScene(prefabHash, prefab);
                node.QueueFree();
                continue;
            }


            if (node is Player)
            {
                PlayerManager.Instance.RegisterPlayer(prefabHash,prefab);
            }

            if (node is ItemComp item)
            {
                ItemManager.Instance.RegisterItem(prefabHash, prefab, item.Data);
            }

            NetObjectManager.Instance.RegisterNetObject(prefabHash, prefab);

            node.QueueFree();
        }

    }

    /// <summary>注：递归加载指定目录下所有 .tscn 资源</summary>
    private void LoadDirectory(string path)
    {
        using DirAccess dir = DirAccess.Open(path);
        if (dir == null)
        {
            CatLog.Err($"[ResourceManager.LoadDirectory] 目录不存在: {path}");
            return;
        }

        // 遍历当前目录下的所有文件
        dir.ListDirBegin();
        while (true)
        {
            string fileName = dir.GetNext();
            if (string.IsNullOrEmpty(fileName)) break;

            string fullPath = path + fileName;

            // 如果是目录，递归进入
            if (dir.CurrentIsDir())
            {
                if (fileName == "." || fileName == "..") continue;
                LoadDirectory(fullPath + "/");
                continue;
            }

            // 只加载 .tscn 文件
            if (!fileName.EndsWith(".tscn")) continue;

            LoadAsset(fullPath);
        }
        dir.ListDirEnd();
    }

    /// <summary> 辅助: 从指定路径加载资源 </summary>
    private void LoadAsset(string res)
    {
        var ps = ResourceLoader.Load<PackedScene>(res);

        if (ps != null)
        {
            _resourceList.Add(ps);
        }
        else
        {
            CatLog.Err($"[ResourceManager.LoadAsset]：资源加载失败资源检查路径：{res}");
        }
    }

}
