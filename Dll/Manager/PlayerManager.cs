using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Manager
{
    public class PlayerManager
    {
        private static PlayerManager _instance;
        public static PlayerManager Instance => _instance ??= new PlayerManager();



        private PackedScene playerPrefab;

        public Dictionary<int, Player> ActivePlayers = [];

        public Player m_LocalPlayer { get; set; }
        public PlayerData m_LocalPlayerData { get; set; }


        private PlayerManager() => playerPrefab ??= NetObjectManager.Instance.GetPrefab("Player");


         

        public void SpawnLocalPlayer(Vector3 spawnPos)
        {
            if (!NetCore.Instance.IsHost) return;  // 只有主机有权 Spawn，客户端会通过 ObjCreate 自动生成

            int hash = CatUtils.GetStableHashCode("Player");
            NetObjectRegistry.Instance.Spawn(hash, spawnPos, Quaternion.Identity, NetCore.Instance.LocalPeerID);
        }


        /// <summary>
        /// 主机为新加入的客户端生成玩家角色（所有权归该客户端）。
        /// 如果连接的是自己（本地玩家），不应该走这里，因为本地玩家已在 MainWorld 中调用 SpawnLocalPlayer。
        /// </summary>
        public void SpawnPlayerForPeer(long peerId)
        {
            // 避免为主机自己重复生成（本地玩家已由 SpawnLocalPlayer 处理）
            if (peerId == NetCore.Instance.LocalPeerID) return;


            // 出生点可以随机，也可以使用配置的出生点
            Vector3 spawnPos = GetRandomSpawnPoint(); // 或从 SpawnPian 读取
            int hash = CatUtils.GetStableHashCode("Player");
            NetObjectRegistry.Instance.Spawn(hash, spawnPos, Quaternion.Identity, peerId);
        }

        // 临时随机出生点（后续可改用场景中的标记）
        private Vector3 GetRandomSpawnPoint()
        {
            return new Vector3(
                (float)GD.RandRange(-5, 5),
                2,
                (float)GD.RandRange(-5, 5)
            );
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

        /// <summary>
        /// 为指定客户端生成玩家（由主机调用）
        /// </summary>
        public void SpawnPlayerForClient(long clientId, Vector3 spawnPos)
        {
            if (!NetCore.Instance.IsHost)
            {
                GD.PrintErr("[PlayerManager] 只有主机有权为客户端生成玩家");
                return;
            }

            int hash = CatUtils.GetStableHashCode("Player");
            NetObjectRegistry.Instance.Spawn(hash, spawnPos, Quaternion.Identity, clientId);
        }

    }
}
