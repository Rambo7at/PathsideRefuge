using Godot;
using Godot.Collections;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Base.SceneBase;

namespace 途畔归所.Dll.Manager;

/// <summary>注：世界管理器，负责场景加载/切换、场景数据持久化、游戏相机管理、全局游戏时间</summary>
public class WorldManager
{
    /// <summary>注：游戏运行状态</summary>
    public enum E_GameState
    {
        Menu = 0,      // 主菜单/角色创建，时间暂停
        Playing = 1,   // 玩家进入世界开始行动，时间推进
    }

    private Dictionary<int, PackedScene> SceneDict = [];          // 场景哈希 → 场景资源
    private Dictionary<int, WorldData> WorldDataDict { get; set; } = [];

    public int SelWorldIdx { get; set; }

    public SceneBase CurrentScene { get; set; }                          // 当前加载的场景

    private Camera3D _gameCamera;                                 // 游戏相机（ViewScene时停用，GameScene由PlayerCamera接管）

    public WorldData CurrentWorld => GetCurrentWorld();
    public bool HasWorlds => WorldDataDict.Count > 0;

    public int CurrentSceneHash => CurrentScene?.SceneData.SceneHash ?? -1;

    /// <summary>注：当前游戏状态（仅 Playing 时时间推进）</summary>
    public E_GameState GameState { get; private set; } = E_GameState.Menu;

    /// <summary>注：时间倍率，1真实秒 = 60游戏秒（1天=24分钟）</summary>
    private const float TIME_RATE = 86400f / 1440f;

    /// <summary>注：新游戏起始时刻（08:00 对应秒数）</summary>
    private const float START_HOUR_SEC = 8f * 3600f;

    /// <summary>注：游戏总秒数（0~86400）</summary>
    private float _totalSec = 0f;

    /// <summary>注：上次日志记录的游戏小时，用于小时变化时打印一次</summary>
    private int _lastLogHour = -1;

    /// <summary>注：当前游戏小时（0-23）</summary>
    public int Hour => (int)(_totalSec / 3600) % 24;

    /// <summary>注：当前游戏分钟（0-59）</summary>
    public int Minute => (int)(_totalSec / 60) % 60;

    private static WorldManager _instance;
    public static WorldManager Instance => _instance ??= new WorldManager();

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

    /// <summary>注：游戏开始，进入 Playing 状态并重置时间到起点（幂等，重复进入世界不重置）</summary>
    public void StartGame()
    {
        if (GameState == E_GameState.Playing) return;

        GameState = E_GameState.Playing;
        _totalSec = START_HOUR_SEC;
        _lastLogHour = -1;
        CatLog.Ok($"[WorldManager] 游戏开始，时间从 {GetTimeString()} 起算");
    }

    /// <summary>注：返回主菜单，时间暂停（触发点待接入主菜单流程）</summary>
    public void ReturnToMenu()
    {
        GameState = E_GameState.Menu;
        CatLog.Ok("[WorldManager] 返回主菜单，时间暂停");
    }

    /// <summary>注：时间推进，仅 Playing 状态生效；由 GameCore 每帧调用</summary>
    public void UpdateTime(float delta)
    {
        if (GameState != E_GameState.Playing) return;

        _totalSec += delta * TIME_RATE;
        if (_totalSec >= 86400f) _totalSec -= 86400f;

        // 游戏小时变化时打印一次（1游戏小时 = 1真实分钟，便于快速测试）
        if (Hour != _lastLogHour)
        {
            _lastLogHour = Hour;
            CatLog.Debug($"[WorldManager] 游戏时间：{GetTimeString()}");
        }
    }

    /// <summary>注：获取当前时间字符串（HH:mm）</summary>
    public string GetTimeString() => $"{Hour:D2}:{Minute:D2}";

    /// <summary>注：获取当前世界数据（如选中索引无效，则自动指向第一个有效世界）</summary>
    private WorldData GetCurrentWorld()
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

    /// <summary>注：保存当前场景数据，返回世界存档（供 SaveManager 调用）</summary>
    public Dictionary<int, WorldData> SaveWorldDataDict()
    {

        SaveSceneData();

        Dictionary<int, WorldData> data = [];

        foreach (var item in WorldDataDict)
        {
            if (item.Value == null) continue;

            data.Add(item.Key, item.Value.DeepCopy());
        }

        return data;
    }


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
