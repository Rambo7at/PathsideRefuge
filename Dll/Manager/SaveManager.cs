using Godot;
using Godot.Collections;
using System;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Utils;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace 途畔归所.Dll.Manager;

/// <summary>注：游戏存档管理。</summary>
public class SaveManager
{
	private static SaveManager _instance;
	public static SaveManager Instance => _instance ??= new SaveManager();

	private const string Path = "res://Save/GameSave.res";
	private SaveData DATA;

	private SaveManager() => Load();

	public void Init() { }

	/// <summary>注：获取所有世界数据字典</summary>
	public Dictionary<int, WorldData> GetWorldDataDict()
	{
		Dictionary<int, WorldData> data = [];

		foreach (var item in DATA.m_worldDataDict)
		{
			if (item.Value == null) continue;
			data.Add(item.Key, item.Value.DeepCopy());
		}

		return data;
	}

	/// <summary>注：获取当前选中的世界索引</summary>
	public int GetSelectedWorldIndex() => DATA.m_selworldIndex;

	/// <summary>注：获取所有玩家数据字典</summary>
	public Dictionary<int, CreatureData> GetPlayerDataDict()
	{
		Dictionary<int, CreatureData> data = [];

		foreach (var item in DATA.m_playerDataDict)
		{
			if (item.Value == null) continue;
			data.Add(item.Key, item.Value.DeepCopy());
		}

		return data;
	}

	/// <summary>注：获取当前选中的玩家索引</summary>
	public int GetSelectedPlayerIndex() => DATA.m_selPlayerIndex;

	/// <summary> 注：加载游戏数据 </summary>
	private void Load()
	{
		if (!FileAccess.FileExists(Path))
		{
			CatLog.Info($"[SaveManager.Load] 目录 {Path} 中未有存档，准备执行新建");
			Save();
			return;
		}

		SaveData data = GD.Load<SaveData>(Path);
		if (data != null)
		{
			DATA = data;
			CatLog.Ok($"[SaveManager.Load] 存档加载成功，玩家数:{DATA.m_playerDataDict?.Count ?? 0}，世界数:{DATA.m_worldDataDict?.Count ?? 0}");
		}
		else
		{
			CatLog.Err($"[SaveManager.Load] 获取的存档数据为空，准备执行新建");
			Save();
		}
	}

	/// <summary>注：保存数据至本地</summary>
	public void Save()
	{
		if (DATA == null)
		{
			DATA = new SaveData();
			ResourceSaver.Save(DATA, Path);
			CatLog.Info($"[SaveManager.Save] 存档 DATA 为空，已创建新 SaveData 并保存至 {Path}");
			return;
		}

		PersistAll();

		try
		{
			ResourceSaver.Save(DATA, Path);
			CatLog.Ok($"[SaveManager.Save] 成功保存至 {Path}");
		}
		catch (Exception ex)
		{
			CatLog.Err($"[SaveManager.Save] 存储异常：{ex}");
		}
	}

	/// <summary>注：更新存档数据</summary>
	private void PersistAll()
	{
		DATA.m_playerDataDict.Clear();
		DATA.m_playerDataDict = PlayerManager.Instance.SavePlayerDataDict();

		DATA.m_worldDataDict.Clear();
		DATA.m_worldDataDict = WorldManager.Instance.SaveWorldDataDict();
	}

}
