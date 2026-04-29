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
        private PackedScene playerPrefab;
        public Dictionary<int, PlayerData> Players = new();

        public PlayerData m_LocalPlayerData;

        /// <summary>注：加载资源</summary>
        /// <param name="packedScene">预制件列表</param>
        public void Init(PackedScene packedScene)
        {
            if (packedScene == null) return;

            if (!(packedScene.Instantiate() is Player)) return;
            playerPrefab = packedScene;
        }


        /// <summary>注：获取实例化玩家</summary>
        /// <returns>Player节点</returns>
        public Player GetPlyaer()
        {
            if (playerPrefab == null) return null;
            var pl = playerPrefab.Instantiate() as Player;
            pl.m_PlayerData = m_LocalPlayerData;

            pl.m_PlayerData.m_LocalPlayer = pl;
            return pl;
        }

    }
}
