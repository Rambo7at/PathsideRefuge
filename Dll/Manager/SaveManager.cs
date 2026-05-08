using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace 途畔归所.Dll.Manager
{
	/// <summary> 注：存档管理器 </summary>
	public class SaveManager
	{
        private static SaveManager _instance;
        public static SaveManager Instance => _instance ??= new SaveManager();

        private const string Path = "res://Save/GameSave.res";
        public SaveData DATA { get; private set; }

        private SaveManager()
        {
            LoadData();
            DebugSaveData();
        }




        /// <summary>注：获取存档全部的玩家ID </summary>
        /// <returns>玩家ID数组</returns>
        public List<int> GetPlayerIDList() => DATA.GetAllPlayerID();

        /// <summary>注：设置选择玩家存档 </summary>
        public void SetPickPlayer(int id) => DATA.PickPlayer = id;

        public PlayerData GetPickPlayerData() => DATA.GetPickPlayerData();

        public void CreatPlayer(string str) => DATA.CreatPlayer(str);


        public void Init() { }



        public Godot.Collections.Dictionary<string, Variant> GetPlacedRuntimeData() => DATA.PlacedRuntimeData;


        public void SavePlacedRuntimeData(string GUID, Godot.Collections.Dictionary<string, Variant> data) => DATA.PlacedRuntimeData[GUID] = data;




        /// <summary> 注：加载游戏数据 </summary>
        private void LoadData()
        {
            if (!FileAccess.FileExists(Path))
            {
                GD.Print($"目录{Path}中未有存档，准备执行新建");
                SaveData();
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
                SaveData();
            }
        }

        /// <summary>注：保存数据至本地</summary>
        public void SaveData()
        {
            if (DATA == null)
            {
                DATA = new SaveData();
                ResourceSaver.Save(DATA, Path);
                return;
            }
            if (UpdateData())
            {
                ResourceSaver.Save(DATA, Path);
                GD.Print("成功保存");
            }

        }

        /// <summary>注：更新存档数据</summary>
        private bool UpdateData()
        {

            foreach (var pl in PlayerManager.Instance.ActivePlayers)
            {
                if (pl.Key == DATA.PickPlayer)
                {
                    var D = DATA.GetPickPlayerData();
                    D = pl.Value.m_PlayerData.DeepCopy();
                }
            }
            return true;
        }

        private void DebugSaveData()
        {

            if (DATA.m_PlyaerDataDict == null)
            {
                GD.Print("[SaveManager.CheckSaveData]：检测[DATA.m_PlyaerDataDict]为空");
                return;
            }

            if (DATA.m_PlyaerDataDict.Count == 0)
            {
                GD.Print("[SaveManager.CheckSaveData]：检测[DATA.m_PlyaerDataDict]数据为空");
                return;
            }

            GD.Print("[SaveManager.CheckSaveData]：准备打印玩家列表----------");

            foreach (var pl in DATA.m_PlyaerDataDict)
            {
                GD.Print($"[SaveManager.CheckSaveData]：玩家ID{pl.Key}");
            }

        }


        public bool IsValidPlayerSaveData()
        {
            if (DATA.m_PlyaerDataDict.Count == 0) return false;

            return true;
        }

    }
}
