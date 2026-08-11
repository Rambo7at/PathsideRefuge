using Godot;
using Godot.Collections;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Base.SceneBase;

namespace 途畔归所.Dll.Manager;

/// <summary>注：世界管理器，负责场景加载/切换、场景数据持久化、游戏相机管理</summary>
public class WorldManager
{
	private static WorldManager _instance;
	public static WorldManager Instance => _instance ??= new WorldManager();

	private Dictionary<int, PackedScene> SceneDict = [];          // 场景哈希 → 场景资源

	private Dictionary<int, WorldData> WorldDataDict { get; set; } = [];

	public int SelWorldIdx { get; set; }

	private SceneBase _currentScene;                              // 当前加载的场景

	private Camera3D _gameCamera;                                 // 游戏相机（ViewScene时停用，GameScene由PlayerCamera接管）

	public WorldData CurrentWorld => GetCurrentWorld();
	public bool HasWorlds => WorldDataDict.Count > 0;

	public int CurrentSceneHash => _currentScene?.SceneData.SceneHash ?? -1;

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

		if (_currentScene == null)
		{
			CatLog.Err("[WorldManager.ChangeScene] 当前场景为空，无法获取场景树执行切换");
			return false;
		}

		SaveSceneData(_currentScene);
		CatLog.Ok($"[WorldManager] 场景切换至：{_currentScene.Name} -> {scene.Name}");

		_currentScene.GetTree().ChangeSceneToNode(scene);
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

		if (!CurrentWorld.SceneDataDict.TryGetValue(scene.SceneData.SceneHash, out var sceneData))
		{
			CurrentWorld.SceneDataDict[scene.SceneData.SceneHash] = scene.SceneData;
			return scene.SceneData;
		}

		return sceneData;
	}

	/// <summary>注：获取游戏相机（仅 GameScene 返回有效）</summary>
	public Camera3D GetCamera()
	{
		if (_currentScene.SceneType != E_SceneType.GameScene)
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

		_currentScene = node3D;
	}

	/// <summary>注：获取当前场景</summary>
	public SceneBase GetCurrentScene() => _currentScene;

	/// <summary>注：获取当前场景哈希</summary>
	public int GetCurrentScenehash() => _currentScene.SceneData?.SceneHash ?? default;

	/// <summary>注：保存当前场景数据，返回世界存档（供 SaveManager 调用）</summary>
	public Dictionary<int, WorldData> SaveWorldDataDict()
	{
		SaveSceneData(_currentScene);

		Dictionary<int, WorldData> data = [];

		foreach (var item in WorldDataDict)
		{
			if (item.Value == null) continue;

			data.Add(item.Key, item.Value.DeepCopy());
		}

		return data;
	}

	/// <summary>注：保存指定场景数据到世界存档，触发场景内所有对象保存状态</summary>
	public void SaveSceneData(SceneBase sceneBase)
	{
		if (sceneBase == null || CurrentWorld == null)
		{
			CatLog.Err("[WorldManager.SaveSceneData]：传入的 SceneData 为空，无法保存。");
			return;
		}

		if (sceneBase.SceneType != E_SceneType.GameScene) return;

		var data = sceneBase.SceneData;
		if (data == null) return;

		CatLog.Debug($"[WorldManager] 开始保存场景：{data.SceneName} (Hash:{data.SceneHash})");

		sceneBase.SaveAllStates();

		CurrentWorld.SceneDataDict[data.SceneHash] = data.DeepCopy();

		CatLog.Ok($"[WorldManager] 场景 {data.SceneName} 数据已保存到世界存档");
	}
}
