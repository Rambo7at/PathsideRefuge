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
            NetObjectRegistry.Instance.Spawn(hash, spawnPos, Quaternion.Identity);
        }





        /// <summary>
        /// 主机为指定 Peer 生成玩家对象（所有权归该 Peer）。
        /// 若 peerId 是主机自己，则忽略（本地玩家已由 SpawnLocalPlayer 处理）。
        /// </summary>
        public void SpawnPlayerForPeer(long peerId, Vector3? spawnPos = null)
        {
            if (peerId == NetCore.Instance.LocalPeerID) return;

            Vector3 pos = spawnPos ?? GetRandomSpawnPoint();
            int hash = CatUtils.GetStableHashCode("Player");
            NetObjectRegistry.Instance.Spawn(hash, pos, Quaternion.Identity, peerId);
        }

        // 临时随机出生点（后续可改用场景中的标记点）
        private Vector3 GetRandomSpawnPoint()
        {
            return new Vector3(
                (float)GD.RandRange(-5, 5),
                2,
                (float)GD.RandRange(-5, 5)
            );
        }

        public int GetActivePlayersIndex() => ActivePlayers.Count;
    }
}
