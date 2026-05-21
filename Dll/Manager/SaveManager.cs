using Godot;
using Godot.Collections;
using System;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Manager
{
	/// <summary>注：负责游戏存档管理，包括数据加载、玩家数据操作等。</summary>
	public class SaveManager
	{
		private static SaveManager _instance;
		public static SaveManager Instance => _instance ??= new SaveManager();

		private const string Path = "res://Save/GameSave.res";
		private SaveData DATA;

		public int m_selPlayerIdx { get => DATA.m_selPlayerIndex == default ? DATA.TryGetValidPlayerDataKey() : DATA.m_selPlayerIndex; set => DATA.m_selPlayerIndex = value; }
		public int m_selWorldIdx { get => DATA.m_selworldIndex == default ? DATA.TryGetValidWorldDataKey() : DATA.m_selworldIndex; set => DATA.m_selworldIndex = value; }
		public Dictionary<int, PlayerData> m_playerDataDict { get => DATA.m_playerDataDict; set => DATA.m_playerDataDict = value; }
		public Dictionary<int, WorldData> m_worldDataDict { get => DATA.m_worldDataDict; set => DATA.m_worldDataDict = value; }
		private SaveManager()
		{

			LoadGameSaveData();

		}

		public void Init() { }

		/// <summary>注：创建新世界并添加到存档数据中。</summary>
		public void CreateWorld(string worldName)
		{
			if (m_worldDataDict == null) return;

			WorldData wdData = new() { m_name = worldName };
			m_worldDataDict.Add(wdData.m_WorldID, wdData);
		}

		/// <summary>注：获取存档中全部有效的世界 ID。</summary>
		public Array<int> GetAllWorldIDs()
		{
			if (m_worldDataDict == null) return null;
			Array<int> ids = [];

			foreach (var wdData in m_worldDataDict)
			{
				if (wdData.Key == default || wdData.Value == null) continue;
				ids.Add(wdData.Key);
			}
			return ids;
		}

		/// <summary>注：获取选中世界的数据。</summary>
		public WorldData GetSelectedWorldData()
		{
			if (m_worldDataDict == null || m_worldDataDict.Count == 0) return null;

            if (m_worldDataDict.TryGetValue(m_selWorldIdx, out WorldData wdData)) return wdData;

			foreach (var data in m_worldDataDict) return data.Value;

            return null;
        }

		/// <summary>注：判断存档是否存在有效的世界存档数据。</summary>
		public bool HasValidWorldSaveData()
		{
			m_worldDataDict ??= [];

			if (m_worldDataDict.Count == 0) return false;
			return true;
		}


		/// <summary>注：获取存档中全部有效的玩家ID。</summary>
		public Array<int> GetAllPlayerIDs()
		{
			if (m_playerDataDict == null) return null;
			Array<int> ints = [];

			foreach (var plData in m_playerDataDict)
			{
				if (plData.Key == default || plData.Value == null) continue;

				ints.Add(plData.Key);
			}
			return ints;
		}

		/// <summary>注：获取选中玩家的数据。</summary>
		public PlayerData GetSelectedPlayerData()
		{
			if (!m_playerDataDict.TryGetValue(m_selPlayerIdx, out PlayerData plData)) return null;
			return plData;
		}

		/// <summary>注：创建新玩家并添加到存档数据中。</summary>
		public void CreatePlayer(string playerName)
		{
			if (m_playerDataDict == null) return;

			PlayerData pldata = new() { m_Name = playerName };

			m_playerDataDict.Add(pldata.m_PlayerID, pldata);
		}

		/// <summary>注：判断存档是否存在有效的玩家存档数据。</summary>
		public bool HasValidPlayerSaveData()
		{
			m_playerDataDict ??= [];

			if (m_playerDataDict.Count == 0) return false;

			return true;
		}


		/// <summary> 注：加载游戏数据 </summary>
		private void LoadGameSaveData()
		{
			if (!FileAccess.FileExists(Path))
			{
				GD.Print($"目录{Path}中未有存档，准备执行新建");
				SaveGameSaveDataToLocal();
				return;
			}

			SaveData data = GD.Load<SaveData>(Path);
			if (data != null)
			{
				DATA = data;
			}
			else
			{
				GD.Print($"获取的存档数据为空，准备执行新建");
				SaveGameSaveDataToLocal();
			}
		}

		/// <summary>注：保存数据至本地</summary>
		public void SaveGameSaveDataToLocal()
		{
			if (DATA == null)
			{
				DATA = new SaveData();
				ResourceSaver.Save(DATA, Path);
				return;
			}

			UpdateGameSaveData();

			try
			{
				ResourceSaver.Save(DATA, Path);
				GD.Print("成功保存");
			}
			catch (Exception ex)
			{
				CatLog.Err($"[SaveManager.SaveData]：存储异常：{ex}");
			}
		}

		/// <summary>注：更新存档数据</summary>
		private void UpdateGameSaveData()
		{
			int id = PlayerManager.Instance.m_LocalPlayerData.m_PlayerID;
			m_playerDataDict[id] = PlayerManager.Instance.m_LocalPlayerData.DeepCopy();
		}

		private void DebugPrintGameSaveData()
		{

			if (m_playerDataDict == null)
			{
				GD.Print("[SaveManager.CheckSaveData]：检测[DATA.m_PlyaerDataDict]为空");
				return;
			}

			if (m_playerDataDict.Count == 0)
			{
				GD.Print("[SaveManager.CheckSaveData]：检测[DATA.m_PlyaerDataDict]数据为空");
				return;
			}

			GD.Print("[SaveManager.CheckSaveData]：准备打印玩家列表----------");

			foreach (var pl in m_playerDataDict)
			{
				GD.Print($"[SaveManager.CheckSaveData]：玩家ID{pl.Key}");
			}

		}

	}
}
