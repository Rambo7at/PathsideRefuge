using Godot;
using System.Collections.Generic;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Manager
{
    public class PlayerManager
    {
        private static PlayerManager _instance;
        public static PlayerManager Instance => _instance ??= new PlayerManager();

        public Dictionary<int, Player> ActivePlayers = [];

        public int m_playerHash;

        public Player m_LocalPlayer;
        public CreatureData m_LocalPlayerData { get; set; }

        public PlayerGUI m_CanvasLayer;


        private PlayerManager()
        {
            m_playerHash = CatUtils.GetStableHashCode("Player");

            var node = NetObjectManager.Instance.GetPrefab(m_playerHash);
            if (node == null) return;

            Player pl = node.Instantiate<Player>();
            if (pl == null) return;

            m_LocalPlayer = pl;

            m_CanvasLayer = m_LocalPlayer.m_playerGUI;

        }

        public void SpawnLocalPlayer(Vector3 Pos, Vector3 rot)
        {
            if (m_LocalPlayerData == null)
            {
                CatLog.Err("[PlayerManager.SpawnLocalPlayer]：检测数据信息 m_LocalPlayerData 是空！");
                return;
            }
            m_LocalPlayer.m_data = m_LocalPlayerData;

            NetObjectManager.Instance.SpawnObject(Pos, rot, default, m_LocalPlayer);
        }

        public int GetActivePlayersIndex() => ActivePlayers.Count;

        public int GetPlayerID() => (m_LocalPlayer?.m_data?.m_playerData == null || m_LocalPlayer?.m_data?.m_playerData.m_playerID == default) ? 0 : m_LocalPlayer.m_data.m_playerData.m_playerID;

    }
}
