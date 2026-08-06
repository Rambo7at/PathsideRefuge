using Godot;
using System.Collections.Generic;
using 维修公司.Dll.data;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Data.SceneData;

namespace 途畔归所.Dll.Manager;

public class WorldManager
{
    private static WorldManager _instance;
    public static WorldManager Instance => _instance ??= new WorldManager();

    private Dictionary<int, PackedScene> SceneDict = [];

    private WorldData _worldData;
    public WorldData m_worldData { get => _worldData ??= SaveManager.Instance.GetSelectedWorldData(); private set => _worldData = value; }

    private SceneBase currentScene;  // 当前加载的场景

    private Camera3D gameCamera;  // 游戏摄像机

    public WorldManager()
    {
        gameCamera ??= new Camera3D
        {
            Name = "GameCamera",
            Current = false
        };
    }


    /// <summary>注：注册场景资源</summary>
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



    /// <summary> 获取场景资源 </summary>
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

        // 修复：SceneType 枚举位于 SceneData 内部
        if (sceneBase.m_sceneData.m_sceneType == SceneData.SceneType.GameScene)
        {
            sceneBase.m_sceneData.m_sceneName = sceneBase.Name;
            sceneBase.m_sceneData.m_sceneHash = hash;
        }

        return sceneBase;
    }

    /// <summary> 切换场景 </summary>
    public bool ChangeScene(string name)
    {
        if (GetPackedScene(CatUtils.GetStableHashCode(name)) is not SceneBase scene) return false;

        CatLog.Ok($"[WorldManager] 场景切换至： {currentScene.Name} -> {name}");

        if (currentScene == null)
        {
            CatLog.Err("[WorldManager.ChangeScene] 当前场景为空，无法获取场景树执行切换。");
            return false;
        }
        currentScene.QueueFree();
        currentScene.GetTree().ChangeSceneToNode(scene);
        return true;
    }

    /// <summary> 加载场景数据 </summary>
    public SceneData LoadSceneData(SceneBase scene)
    {
        if (m_worldData == null)
        {
            CatLog.Err("[WorldManager.LoadSaveData]：WorldManager没有存档数据，但是触发了加载场景，问题严重，请排查");
            return null;
        }

        if (!m_worldData.m_sceneDataDict.TryGetValue(scene.m_sceneData.m_sceneHash, out var sceneData))
        {
            m_worldData.m_sceneDataDict[scene.m_sceneData.m_sceneHash] = scene.m_sceneData;
            return scene.m_sceneData;
        }

        return sceneData;
    }

    /// <summary>注：获取相机</summary>
    public Camera3D GetCamera()
    {
        if (currentScene.m_sceneData.m_sceneType != SceneType.GameScene)
        {
            return null;
        }
        return gameCamera;
    }

    /// <summary>注：场景根节点在 _Ready 时调用，汇报当前场景</summary>
    public void SetCurrentSceneType(SceneBase node3D)
    {
        if (node3D == null) return;

        if (node3D.m_sceneData.m_sceneType == SceneData.SceneType.ViewScene)
        {
            Node parent = gameCamera.GetParent();
            if (parent != null)
            {
                parent.RemoveChild(gameCamera);
            }
            gameCamera.Current = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }

        currentScene = node3D;
    }

    /// <summary>注：获取当前场景</summary>
    public SceneBase GetCurrentScene() => currentScene;

    /// <summary>注：获取当前场景hash</summary>
    public int GetCurrentScenehash() => currentScene.m_sceneData.m_sceneHash;




    /// <summary> 保存当前场景数据到世界 </summary>
    public void PersistScene(SceneBase sceneBase)
    {
        if (sceneBase == null)
        {
            CatLog.Err("[WorldManager.SaveSceneData]：传入的 SceneData 为空，无法保存。");
            return;
        }

        if (m_worldData == null) return;

        var data = sceneBase.m_sceneData;
        if (data == null) return;
        if (data.m_sceneType != SceneData.SceneType.GameScene) return;

        sceneBase.FlushNetStates();

        var objarr = NetObjectRegistry.Instance.GetNetObjectsForCurrentScene(data.m_sceneHash);

        if (objarr != null && objarr.Count != 0)
        {
            data.m_NetObjectArr.Clear();
            foreach (var netojbs in objarr)
            {
                CatLog.Warn("保存的哈希对象:" + netojbs.PrefabHash);
                data.m_NetObjectArr.Add(netojbs);
            }
        }

        CatLog.Warn("保存的对象是否是新场景:" + data.m_newScene);


        m_worldData.m_sceneDataDict[data.m_sceneHash] = data.DeepCopy();
        CatLog.Debug($"[WorldManager] 场景 {data.m_sceneName} (Hash:{data.m_sceneHash}) 数据已写入世界。");
    }


    public WorldData PersistCurrentScene()
    {
        PersistScene(currentScene);

        return _worldData;
    }



}


