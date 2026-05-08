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

        private Player m_LocalPlayers;

        /// <summary>注：加载资源</summary>
        public void Init()
        {
            if (ResourceManager.Instance.m_PlayerPrefab == null) return;
            playerPrefab = ResourceManager.Instance.m_PlayerPrefab;
        }


        /// <summary> 注：获取本地玩家 </summary>
        /// <param name="player"></param>
        /// <returns></returns>
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

        public Player GetLocalPlayer()
        {
            foreach (var item in ActivePlayers)
            {
                if (item.Value != null)
                {
                    return item.Value;
                }
            }

            return null;
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

        public int GetActivePlayersIndex() => ActivePlayers.Count;

        public void SpawnRemotePlayer(long peerId)
        {
            Player pl = playerPrefab.Instantiate() as Player;
            pl.Name = $"Player_{peerId}";
            pl.SetMultiplayerAuthority((int)peerId);  // 设置网络所有权

            
            // 添加到场景
            GameCore.Instance.GetTree().CurrentScene.AddChild(pl);
            ActivePlayers[(int)peerId] = pl;
            pl.GlobalPosition = new Vector3(0, 2, 0);  // 临时出生点
        }

    }
}
