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
        public Dictionary<long, PlayerData> Players = new();

        // 本地玩家存档
        public List<PlayerData> LocalPlayers = [];

        private PackedScene playerPrefab;

        public PlayerManager()
        {
            Players.Clear();
            LocalPlayers.Clear();
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
            var pl = new PlayerData() { Name = name, };

            LocalPlayers.Add(pl);

            GameCore.Instance.m_SaveManager.UpdateData();
            GameCore.Instance.m_SaveManager.SaveData();
        }



        public List<PlayerData> GetLocalPlayerData()
        { 
            if (LocalPlayers == null || LocalPlayers.Count == 0) return null;
            return LocalPlayers; 
        }

        /// <summary>注：获取实例化玩家</summary>
        /// <returns>Player节点</returns>
        public Player GetPlyaer()
        {
            if (playerPrefab == null) return null;
            return playerPrefab.Instantiate() as Player;
        }

    }
}
