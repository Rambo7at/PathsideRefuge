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
		public CreatureData m_LocalPlayerData { get ; set; }

		private PlayerManager()
		{
			m_playerHash = CatUtils.GetStableHashCode("Player");

			if (NetObjectManager.Instance.GetPrefab(m_playerHash).Instantiate() is not Player pl) return;

			m_LocalPlayer = pl;
		}

        public void SpawnLocalPlayer(Vector3 Pos, Vector3 rot)
        {
            CatLog.Debug($"[PlayerManager.SpawnLocalPlayer] 进入，m_LocalPlayer={(m_LocalPlayer == null ? "null" : "存在")}");

            // 1. 检查玩家是否有效，无效则重新实例化
            if (m_LocalPlayer == null || !GodotObject.IsInstanceValid(m_LocalPlayer))
            {
                CatLog.Warn("[PlayerManager.SpawnLocalPlayer] 玩家引用无效，重新实例化");
                var prefab = NetObjectManager.Instance.GetPrefab(m_playerHash);
                if (prefab == null)
                {
                    CatLog.Err("[PlayerManager.SpawnLocalPlayer] 无法获取玩家预制体");
                    return;
                }
                var newPlayer = prefab.Instantiate() as Player;
                if (newPlayer == null)
                {
                    CatLog.Err("[PlayerManager.SpawnLocalPlayer] 实例化玩家失败");
                    return;
                }
                m_LocalPlayer = newPlayer;
                CatLog.Ok("[PlayerManager.SpawnLocalPlayer] 已重新实例化玩家");
            }
            else
            {
                // 玩家有效，但如果还有父节点（异常情况），先移除
                if (m_LocalPlayer.GetParent() != null)
                {
                    CatLog.Warn($"[PlayerManager.SpawnLocalPlayer] 玩家仍有父节点 {m_LocalPlayer.GetParent().Name}，正在移除");
                    m_LocalPlayer.GetParent().RemoveChild(m_LocalPlayer);
                }
            }

            // 2. 检查玩家数据
            if (m_LocalPlayerData == null)
            {
                CatLog.Err("[PlayerManager.SpawnLocalPlayer] m_LocalPlayerData 为空");
                return;
            }

            // 3. 设置数据并添加到场景
            m_LocalPlayer.m_CreatureData = m_LocalPlayerData;
            CatLog.Debug($"[PlayerManager.SpawnLocalPlayer] 调用 SpawnObject，位置：{Pos}");
            NetObjectManager.Instance.SpawnObject(Pos, rot, default, m_LocalPlayer);
            CatLog.Ok("[PlayerManager.SpawnLocalPlayer] 执行完成");
        }

        public int GetActivePlayersIndex() => ActivePlayers.Count;

		public int GetPlayerID() => (m_LocalPlayerData?.PlayerID == default) ? 0 : m_LocalPlayerData.PlayerID;

	}
}
