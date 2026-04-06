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


        public SaveManager() => Init();

        public void Init()
        {

            if (!LoadData()) SaveData();
            LoadSaveDataToPlayer();
        }

        public bool LoadData()
        {
            if (FileAccess.FileExists(Path) == false) return false;

            var data = GD.Load<SaveData>(Path);
            if (data != null) DATA = data;
            return true;
        } 

        public void SaveData()
        {
            if (DATA == null) DATA = new SaveData();

            ResourceSaver.Save(DATA, Path);
        }

        public void UpdateData()
        {
            if (GameCore.Instance.m_PlayerManager.LocalPlayers == null || GameCore.Instance.m_PlayerManager.LocalPlayers.Count == 0) return;


            for (int i = 0; i < GameCore.Instance.m_PlayerManager.LocalPlayers.Count; i++)
            {
                if (i >= DATA.playerDatas.Count)
                {
                    DATA.playerDatas.Add(GameCore.Instance.m_PlayerManager.LocalPlayers[i]);
                }
                else
                {
                    DATA.playerDatas[i] = GameCore.Instance.m_PlayerManager.LocalPlayers[i];
                }
            }
        }

        private void LoadSaveDataToPlayer()
        {
            if (DATA.playerDatas == null && DATA.playerDatas.Count == 0) return;

            foreach (var data in DATA.playerDatas)
            {
                GameCore.Instance.m_PlayerManager.LocalPlayers.Add(data);
            }
        }
    }
}