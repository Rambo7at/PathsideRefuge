using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Data;

namespace 途畔归所.Dll.Manager
{
    public class PlayerManager
    {

        // 所有在线玩家
        public Dictionary<long, PlayerData> Players = new();

        // 本地玩家存档
        public List<PlayerData> LocalPlayer = [];

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
            playerPrefab = packedScene;
        }


        /// <summary>注：创建角色</summary>
        /// <param name="name"></param>
        public void Creator(string name) => LocalPlayer.Add(new PlayerData() { Name = name, });



        public List<PlayerData> GetLocalPlayerData()
        { 
            if (LocalPlayer == null || LocalPlayer.Count == 0) return null;
            return LocalPlayer; 
        }



    }
}
