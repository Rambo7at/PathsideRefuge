using Godot;
using Godot.Collections;
using System;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Manager;

/// <summary>注：玩家管理器，负责玩家数据的持有、管理、生成及场景切换入口</summary>
public class PlayerManager
{
	private static PlayerManager _instance;
	public static PlayerManager Instance => _instance ??= new PlayerManager();

	private PackedScene _playerPacked;                              // 缓存的玩家预制体

	private Dictionary<int, CreatureData> PlayerDataDict { get; set; } = [];  // 所有玩家数据

	public int SelPlayerIdx { get; set; }                          // 当前选中的玩家索引

	public int PlayerHash { get; private set; }                    // 玩家预制体哈希值

	public bool HasPlayers => PlayerDataDict.Count > 0;            // 是否存在玩家数据

	public Player LocalPlayer { get; set; }                        // 本地玩家实例
	public CreatureData LocalPlayerData => GetLocalPlayerData();   // 本地玩家数据（只读）

	public bool IsHost => NetCore.Instance.IsHost;                  // 是否为主机


	private PlayerManager()
	{
		PlayerDataDict = SaveManager.Instance.GetPlayerDataDict();
		SelPlayerIdx = SaveManager.Instance.GetSelectedPlayerIndex();


		CatLog.Ok("[PlayerManager] 初始化完成");
	}

	/// <summary>注：注册玩家预制体及其哈希值</summary>
	public void RegisterPlayer(int hash, PackedScene packedScene)
	{
		_playerPacked = packedScene;
		PlayerHash = hash;
	}

	/// <summary>注：获取所有有效的玩家ID列表</summary>
	public Array<int> GetAllPlayerIDs()
	{
		Array<int> ints = [];

		foreach (var plData in PlayerDataDict)
		{
			if (plData.Key == default || plData.Value == null) continue;
			ints.Add(plData.Key);
		}
		return ints;
	}

	/// <summary>注：创建新玩家并添加到数据字典中</summary>
	public void CreatePlayer(string playerName)
	{
		CreatureData data = new()
		{
			Name = playerName,
			IsPlayer = true,
			PlayerID = Math.Abs(Guid.NewGuid().GetHashCode())
		};

		PlayerDataDict.Add(data.PlayerID, data);
	}

	/// <summary>注：获取本地玩家数据（如选中索引无效则自动指向第一个有效玩家）</summary>
	public CreatureData GetLocalPlayerData()
	{
		if (PlayerDataDict.TryGetValue(SelPlayerIdx, out var worldData)) return worldData;
		if (PlayerDataDict.Count == 0) return null;

		foreach (var data in PlayerDataDict)
		{
			if (data.Value == null) continue;
			SelPlayerIdx = data.Key;
			return data.Value;
		}
		return null;
	}

	/// <summary>注：导出所有玩家数据的深拷贝（供 SaveManager 写入磁盘）</summary>
	public Dictionary<int, CreatureData> SavePlayerDataDict()
	{
		Dictionary<int, CreatureData> data = [];

		foreach (var item in PlayerDataDict)
		{
			if (item.Value == null) continue;
			data.Add(item.Key, item.Value.DeepCopy());
		}

		return data;
	}



	/// <summary>注：在指定位置生成本地玩家（若玩家实例无效则自动重建）</summary>
	public void SpawnLocalPlayer(Vector3 Pos, Vector3 rot)
	{
		if (LocalPlayer == null || !GodotObject.IsInstanceValid(LocalPlayer))
		{
			if (_playerPacked.Instantiate() is not Player newPlayer) return;
			LocalPlayer = newPlayer;
		}
		else
		{
			if (LocalPlayer.GetParent() != null)
			{
				CatLog.Warn($"[PlayerManager.SpawnLocalPlayer] 玩家仍有父节点 {LocalPlayer.GetParent().Name}，正在移除");
				LocalPlayer.GetParent().RemoveChild(LocalPlayer);
			}
		}

		if (LocalPlayerData == null)
		{
			CatLog.Net("[PlayerManager.SpawnLocalPlayer] LocalPlayerData 为空");
			return;
		}

		LocalPlayer.m_CreatureData = LocalPlayerData.DeepCopy();
		NetObjectManager.Instance.SpawnObject(LocalPlayer,Pos, rot);
	}



	public void SaveLocalPlayerData()
	{
		if (LocalPlayer == null) return;

		if (!PlayerDataDict.ContainsKey(LocalPlayer.m_CreatureData.PlayerID)) return;

		PlayerDataDict[LocalPlayer.m_CreatureData.PlayerID] = LocalPlayer.m_CreatureData.DeepCopy();
	}


	/// <summary>注：检查玩家存档中是否有可用的位置信息（场景匹配且有位置）</summary>
	public bool CanUseSavedPosition() => LocalPlayerData?.LastPosition != default && LocalPlayerData?.LastSceneHash == WorldManager.Instance?.GetCurrentScenehash();

	/// <summary>注：从当前世界状态刷新玩家存档数据（场景哈希、位置、旋转）</summary>
	public void RefreshPlayerData()
	{
		if (LocalPlayerData == null)
		{
			CatLog.Warn("[PlayerManager.RefreshPlayerData] LocalPlayerData 为空，跳过刷新");
			return;
		}

		CatLog.Debug("[PlayerManager.RefreshPlayerData] 开始刷新玩家数据");

		var currentScene = WorldManager.Instance.GetCurrentScene();
		if (currentScene != null)
		{
			LocalPlayerData.LastSceneHash = currentScene.SceneData.SceneHash;
			CatLog.Debug($"[PlayerManager.RefreshPlayerData] 场景哈希已更新：{LocalPlayerData.LastSceneHash}");
		}
		else
		{
			CatLog.Warn("[PlayerManager.RefreshPlayerData] 当前场景为空，无法更新场景哈希");
		}

		if (LocalPlayer != null && GodotObject.IsInstanceValid(LocalPlayer))
		{
			LocalPlayerData.LastPosition = LocalPlayer.GlobalPosition;
			LocalPlayerData.LastRotation = LocalPlayer.GlobalRotation;
			CatLog.Debug($"[PlayerManager.RefreshPlayerData] 位置已更新：{LocalPlayerData.LastPosition}，旋转：{LocalPlayerData.LastRotation}");
		}
		else
		{
			CatLog.Warn("[PlayerManager.RefreshPlayerData] 玩家实例无效，无法更新位置和旋转");
		}

		CatLog.Ok("[PlayerManager.RefreshPlayerData] 玩家数据刷新完成");
	}
}
