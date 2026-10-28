using Godot;
using Godot.Collections;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.System;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Base.SceneBase;

namespace 途畔归所.Dll.Manager;

/// <summary>注：世界管理器，负责场景加载/切换、场景数据持久化、游戏相机管理；时间系统由 System.Time 负责，本类只做存档注入与回写的胶水层</summary>
public class WorldManager
{
    /// <summary>注：场景哈希 → 场景资源的映射字典，供场景切换时通过哈希快速查找</summary>
    private Dictionary<int, PackedScene> SceneDict = [];


    /// <summary>注：世界ID → 世界数据的映射字典，包含所有存档世界</summary>
    private Dictionary<int, WorldData> WorldDataDict { get; set; } = [];

    /// <summary>注：当前选中的世界索引</summary>
    public int SelWorldIdx { get; set; }

    /// <summary>注：当前加载的场景</summary>
    public SceneBase CurrentScene { get; set; }

    /// <summary>注：游戏相机（ViewScene时停用，GameScene由PlayerCamera接管）</summary>
    private Camera3D _gameCamera;

    /// <summary>注：当前选中的世界数据（索引无效时自动指向第一个有效世界）</summary>
    public WorldData CurrentWorld => GetCurrentWorld();

    /// <summary>注：是否存在世界存档</summary>
    public bool HasWorlds => WorldDataDict.Count > 0;

    /// <summary>注：当前场景哈希（无场景时返回 -1）</summary>
    public int CurrentSceneHash => CurrentScene?.SceneData.SceneHash ?? -1;

    /// <summary>注：单例实例</summary>
    private static WorldManager _instance;
    /// <summary>注：单例访问入口，首次访问时自动创建</summary>
    public static WorldManager Instance => _instance ??= new WorldManager();

    /// <summary>注：构造函数，初始化游戏相机并从存档加载世界数据与选中索引</summary>
    public WorldManager()
    {
        _gameCamera ??= new Camera3D
        {
            Name = "_gameCamera",
            Current = false
        };

        WorldDataDict = SaveManager.Instance.GetWorldDataDict();
        SelWorldIdx = SaveManager.Instance.GetSelectedWorldIndex();

        CatLog.Ok("[WorldManager] 初始化完成");
    }

    /// <summary>注：注册场景资源，供后续通过哈希获取</summary>
    public void RegisterScene(int hash, PackedScene packedScene)
    {
        if (packedScene == null) return;

        if (SceneDict.ContainsKey(hash))
        {
            CatLog.Warn($"[WorldManager.RegisterScene]：哈希：{hash}，已经完成注册 跳过");
            return;
        }

        SceneDict[hash] = packedScene;
    }

    /// <summary>注：游戏开始——从当前世界存档注入时间到 Time 系统，再启动时间推进（幂等，重复进入不重置）</summary>
    public void StartGame()
    {
        if (CurrentWorld != null)
        {
            GameTime.Instance.SetTime(CurrentWorld.TimeOfDay > 0f ? CurrentWorld.TimeOfDay : GameTime.START_HOUR_SEC);
        }
        GameTime.Instance.StartGame();
    }

    /// <summary>注：返回主菜单，时间暂停（触发点待接入主菜单流程）</summary>
    public void ReturnToMenu() => GameTime.Instance.ReturnToMenu();

    /// <summary>注：获取当前世界数据（如选中索引无效，则自动指向第一个有效世界）</summary>
    public WorldData GetCurrentWorld()
    {
        if (WorldDataDict.TryGetValue(SelWorldIdx, out var worldData)) return worldData;
        if (WorldDataDict.Count == 0) return null;

        foreach (var data in WorldDataDict)
        {
            if (data.Value == null) continue;
            SelWorldIdx = data.Key;
            return data.Value;
        }
        return null;
    }

    /// <summary>注：创建新世界</summary>
    public void CreateWorld(string worldName)
    {
        WorldData wdData = new() { Name = worldName };
        WorldDataDict.Add(wdData.WorldID, wdData);
    }

    /// <summary>注：获取所有世界ID列表</summary>
    public Array<int> GetAllWorldIDs()
    {
        Array<int> ids = [];
        foreach (var wdData in WorldDataDict)
        {
            if (wdData.Key == default || wdData.Value == null) continue;
            ids.Add(wdData.Key);
        }
        return ids;
    }

    public Dictionary<int, string> GetAllWorldNames()
    {
        Dictionary<int, string> dict = [];

        foreach (var wdData in WorldDataDict)
        {
            if (wdData.Key == default || wdData.Value == null) continue;

            dict.Add(wdData.Key, wdData.Value.Name);
        }

        return dict;
    }


    public bool IsValidWorldName(string worldName)
    {
        foreach (var wdData in WorldDataDict)
        {
            if (wdData.Value.Name != worldName)
            {
                return true;
            }
        }

        return false;

    }


    /// <summary>注：验证指定ID的玩家存档是否存在</summary>
    public bool IsValidWorld(int worldID) => WorldDataDict.TryGetValue(worldID, out var _);

    /// <summary>注：删除指定ID的玩家存档数据</summary>
    public bool RemoveWorldData(int worldID)
    {
        if (!WorldDataDict.TryGetValue(worldID, out var data)) return false;

        WorldDataDict.Remove(worldID);
        return true;
    }


    /// <summary>注：根据哈希获取场景实例，并补全场景数据中的名称和哈希</summary>
    public SceneBase GetPackedScene(int hash)
    {
        if (!SceneDict.TryGetValue(hash, out var packedScene))
        {
            CatLog.Err("[SceneManager.GetPackedScene]：未有获取到对应的场景");
            return null;
        }

        if (packedScene.Instantiate() is not SceneBase sceneBase)
        {
            CatLog.Err($"[SceneManager.GetPackedScene]：查询哈希值{hash}-非游戏场景-资源路径：{packedScene.ResourcePath}");
            return null;
        }

        if (sceneBase.SceneType == E_SceneType.GameScene)
        {
            sceneBase.SceneData.SceneName = sceneBase.Name;
            sceneBase.SceneData.SceneHash = hash;
            CatLog.Warn($"执行哈希载入，当前 SceneHash = {sceneBase.SceneData.SceneHash}");

        }

        return sceneBase;
    }

    /// <summary>注：通过场景名称切换场景（便利方法）</summary>
    public bool ChangeScene(string name)
    {
        if (ChangeScene(CatUtils.GetStableHashCode(name)))
        {
            return true;
        }
        else
        {
            CatLog.Err($"[WorldManager.ChangeScene] 场景切换失败，名称：{name}");
            return false;
        }
    }

    /// <summary>注：通过场景哈希切换场景（核心逻辑），旧场景由 Godot 自动销毁</summary>
    public bool ChangeScene(int sceneHash)
    {
        if (GetPackedScene(sceneHash) is not SceneBase scene)
        {
            CatLog.Err($"[WorldManager.ChangeScene] 获取场景资源失败，哈希：{sceneHash}");
            return false;
        }

        if (CurrentScene == null)
        {
            CatLog.Err("[WorldManager.ChangeScene] 当前场景为空，无法获取场景树执行切换");
            return false;
        }

        CatLog.Ok($"[WorldManager] 场景切换至：{CurrentScene.Name} -> {scene.Name}");

        CurrentScene.GetTree().ChangeSceneToNode(scene);
        return true;
    }

    /// <summary>注：加载默认游戏场景（当玩家存档中无场景哈希时调用）</summary>
    public bool LoadDefaultScene()
    {
        const string defaultScene = "测试场景";
        return ChangeScene(defaultScene);
    }


    /// <summary>注：从世界存档中加载指定场景的数据，若不存在则初始化新数据</summary>
    public SceneData LoadSceneData(SceneBase scene)
    {
        if (CurrentWorld == null)
        {
            CatLog.Err("[WorldManager.LoadSaveData]：WorldManager没有存档数据，但是触发了加载场景，问题严重，请排查");
            return null;
        }

        if (CurrentWorld.SceneDataDict.TryGetValue(scene.SceneData.SceneHash, out var sceneData))
        {
            return sceneData;
        }

        return null;
    }

    /// <summary>注：从世界存档中加载指定场景的数据，若不存在则初始化新数据</summary>
    public SceneData LoadSceneData(int sceneHash)
    {
        if (!CurrentWorld.SceneDataDict.TryGetValue(sceneHash, out var sceneData)) return null;
        return sceneData;
    }

    /// <summary>注：获取游戏相机（仅 GameScene 返回有效）</summary>
    public Camera3D GetCamera()
    {
        if (CurrentScene.SceneType != E_SceneType.GameScene)
        {
            return null;
        }
        return _gameCamera;
    }

    /// <summary>注：由SceneBase节点在 _EnterTree 时调用，更新当前场景引用；ViewScene 时自动停用游戏相机</summary>
    public void SetCurrentSceneType(SceneBase node3D)
    {
        if (node3D == null) return;

        if (node3D.SceneType == E_SceneType.ViewScene)
        {
            Node parent = _gameCamera.GetParent();
            if (parent != null)
            {
                parent.RemoveChild(_gameCamera);
            }
            _gameCamera.Current = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }

        CurrentScene = node3D;
    }

    /// <summary>注：获取当前场景</summary>
    public SceneBase GetCurrentScene() => CurrentScene;

    /// <summary>注：获取当前场景哈希</summary>
    public int GetCurrentScenehash() => CurrentScene.SceneData?.SceneHash ?? default;

    /// <summary>注：保存当前场景数据，返回世界存档（供 SaveManager 调用）；保存前先把 Time 系统当前时间回写到 CurrentWorld.TimeOfDay</summary>
    public Dictionary<int, WorldData> SaveWorldDataDict()
    {
        if (CurrentWorld != null)
        {
            CurrentWorld.TimeOfDay = GameTime.Instance.GetTime();
        }

        SaveSceneData();

        Dictionary<int, WorldData> data = [];

        foreach (var item in WorldDataDict)
        {
            if (item.Value == null) continue;

            data.Add(item.Key, item.Value.DeepCopy());
        }

        return data;
    }


    /// <summary>注：保存当前场景的网络对象数据到世界存档，按场景哈希分组后深拷贝写入 NetObjectList</summary>
    public void SaveSceneData()
    {
        if (CurrentWorld == null) return;


        var netObjectDict = NetObjectRegistry.Instance.GetNetObjectsDict();
        if (netObjectDict.Count == 0) return;

        Dictionary<int, Array<NetObject>> newSceneData = [];

        foreach (var kvp in netObjectDict)
        {
            int sceneHash = kvp.Key.SceneHash;
            var netObjCopy = kvp.Value.DeepCopy();

            if (!newSceneData.ContainsKey(sceneHash))
            {
                newSceneData[sceneHash] = [];
            }
            newSceneData[sceneHash].Add(netObjCopy);
        }

        foreach (var data in newSceneData)
        {
            if (!CurrentWorld.SceneDataDict.TryGetValue(data.Key, out _))
            {
                CurrentWorld.SceneDataDict[data.Key] = new SceneData()
                {
                    SceneHash = data.Key,
                };
            }

            CurrentWorld.SceneDataDict[data.Key].NetObjectList.Clear();
            CurrentWorld.SceneDataDict[data.Key].NetObjectList.AddRange(data.Value);
        }
    }
}
