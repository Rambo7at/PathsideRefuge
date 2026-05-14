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

		public Dictionary<int, Player> ActivePlayers = [];

		private int m_playerHash;
		public Player m_LocalPlayer;
		public PlayerData m_LocalPlayerData { get; set; }


		private PlayerManager()
		{
			m_playerHash = CatUtils.GetStableHashCode("Player");

			var node = NetObjectManager.Instance.GetPrefab(m_playerHash);
			if (node == null) return;

			Player pl = node.Instantiate<Player>();
			if (pl == null) return;

			m_LocalPlayer = pl;
		}

		public void SpawnLocalPlayer(Vector3 Pos, Vector3 rot)
		{
			if (m_LocalPlayerData == null)
			{
				CatLog.Err("[PlayerManager.SpawnLocalPlayer]：检测数据信息 m_LocalPlayerData 是空！");
				return;
			}
			m_LocalPlayer.m_PlayerData = m_LocalPlayerData;
			NetObjectManager.Instance.SpawnObject(Pos, rot, default,m_LocalPlayer);
		}

		public int GetActivePlayersIndex() => ActivePlayers.Count;
	}
}
