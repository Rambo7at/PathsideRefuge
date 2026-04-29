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
        private static PlayerManager _instance;
        public static PlayerManager Instance => _instance ??= new PlayerManager();

        private PlayerManager() { }

        private PackedScene playerPrefab;

        public Dictionary<int, Player> ActivePlayers = [];


        public Player GetLocalPlayer(Player player)
        {
            foreach (var data in ActivePlayers)
            {
                if (data.Key == player.m_PlayerData.m_PlayerID)
                {
                    return data.Value;
                }
            }
            return null;
        }



        /// <summary>注：加载资源</summary>
        /// <param name="packedScene">预制件列表</param>
        public void Init()
        {
            if (ResourceManager.Instance.m_PlayerPrefab == null) return;
            playerPrefab = ResourceManager.Instance.m_PlayerPrefab;
        }


        /// <summary>注：获取实例化玩家</summary>
        /// <returns>Player节点</returns>
        public Player GetPlyaer()
        {
            if (playerPrefab == null) return null;
            var pl = playerPrefab.Instantiate() as Player;
            pl.m_PlayerData = SaveManager.Instance.DATA.GetPickPlayerData();
            return pl;
        }

    }
}
