using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;

namespace 途畔归所.Dll.Manager
{
    /// <summary> 注：存档管理器 </summary>
    public class SaveManager
    {
        public SaveData DATA;

        /// <summary> 注：存档路径 </summary>
        private const string Path = "res://Save/GameSave.res";


        public SaveManager()
        {
            if (!LoadData()) SaveData();
            LoadDataToPlayer();
        }

        /// <summary>注：加载本地存档</summary>
        /// <returns>是否加载成功</returns>
        public bool LoadData()
        {
            if (FileAccess.FileExists(Path) == false) return false;

            var data = GD.Load<SaveData>(Path);
            if (data != null) DATA = data;
            return true;
        }

        /// <summary>注：保存数据至本地</summary>
        public void SaveData()
        {
            if (DATA == null) DATA = new SaveData();
            UpdateData();
            ResourceSaver.Save(DATA, Path);
            GD.Print("成功保存");
        }

        /// <summary>注：更新存档数据</summary>
        private void UpdateData()
        {
            if (GameCore.Instance.m_PlayerManager.LocalPlayerSaves == null || GameCore.Instance.m_PlayerManager.LocalPlayerSaves.Count == 0) return;
            for (int i = 0; i < GameCore.Instance.m_PlayerManager.LocalPlayerSaves.Count; i++)
            {
                if (i >= DATA.playerDatas.Count)
                {
                    DATA.playerDatas.Add(GameCore.Instance.m_PlayerManager.LocalPlayerSaves[i]);
                }
                else
                {
                    DATA.playerDatas[i] = GameCore.Instance.m_PlayerManager.LocalPlayerSaves[i];
                }
            }
        }

        private void LoadDataToPlayer()
        {
            if (DATA.playerDatas == null && DATA.playerDatas.Count == 0) return;

            foreach (var data in DATA.playerDatas)
            {
                GameCore.Instance.m_PlayerManager.LocalPlayerSaves.Add(data);
            }

            GameCore.Instance.m_PlayerManager.m_LocalPlayerData = GameCore.Instance.m_PlayerManager.LocalPlayerSaves[0];
        }
    }
}