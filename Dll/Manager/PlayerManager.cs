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
    public class PlayerManager
    {

        // 所有在线玩家
        public Dictionary<int, PlayerData> Players = new();

        // 本地玩家存档
        public List<PlayerData> LocalPlayerSaves = [];

        public Player m_LocalPlayer;
        public PlayerData m_LocalPlayerData;

        private PackedScene playerPrefab;

        public PlayerManager()
        {
            Players.Clear();
            LocalPlayerSaves.Clear();
        }


        /// <summary>注：加载资源</summary>
        /// <param name="packedScene">预制件列表</param>
        public void Init(PackedScene packedScene)
        {
            if (packedScene == null) return;

            if (!(packedScene.Instantiate() is Player)) return;
            playerPrefab = packedScene;
        }


        /// <summary>注：创建角色</summary>
        /// <param name="name"></param>
        public void Creator(string name)
        {
            var pl = new PlayerData() { m_Name = name, };

            LocalPlayerSaves.Add(pl);
            GameCore.Instance.m_SaveManager.SaveData();
        }



        public List<PlayerData> GetLocalPlayerData()
        { 
            if (LocalPlayerSaves == null || LocalPlayerSaves.Count == 0) return null;
            return LocalPlayerSaves; 
        }

        /// <summary>注：获取实例化玩家</summary>
        /// <returns>Player节点</returns>
        public Player GetPlyaer()
        {
            if (playerPrefab == null) return null;
            var pl = playerPrefab.Instantiate() as Player;
            return pl;
        }

    }
}
