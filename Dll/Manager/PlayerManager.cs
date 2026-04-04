using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 维修公司.Utils;

namespace 维修公司.Dll.Manager
{
    public class PlayerManager
    {

        // 所有在线玩家
        public Dictionary<long, Player> Players = new();

        // 本地玩家存档
        public List<Player> LocalPlayer = [];

        private PackedScene playerPrefab;

        public PlayerManager()
        {
            Players.Clear();

            // 暂时不写查找存档

        }


        /// <summary>注：加载资源</summary>
        /// <param name="packedScene">预制件列表</param>
        public void Init(PackedScene packedScene)
        {
            if (packedScene == null) return;

            if (!(packedScene.Instantiate() is Player)) return;
            GD.Print($"检测到玩家预制件");
            playerPrefab = packedScene;
        }

        public void Creator(string name)
        {
            var pl = playerPrefab.Instantiate() as Player;
            pl.CreatureName = name;
            LocalPlayer.Add(pl);
        }



        public List<Player> GetLocalPlayerData()
        { 
            if (LocalPlayer == null || LocalPlayer.Count == 0) return null;

            return LocalPlayer; 
        }



    }
}
