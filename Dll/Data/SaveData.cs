using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace 途畔归所.Dll.Data
{

	public partial class SaveData : Resource
	{

		[Export] public Array<PlayerData> playerDatas = [];

        [Export] public int PickPlayer = 0;

		public PlayerData GetPickPlayerData()
		{ 
		   if (playerDatas == null || playerDatas.Count == 0) return null;

			if (PickPlayer == 0)
			{
				PickPlayer = playerDatas[0].m_PlayerID;
                return playerDatas[0];
            }

            foreach (var Pl in playerDatas)
            {
				if (Pl.m_PlayerID == PickPlayer)
				{
                    return Pl;
                }
            }

            return playerDatas[0];
        }


        public void PickNextPlayer()
        {
            if (playerDatas == null || playerDatas.Count <= 1)
                return;

            int currentIndex = -1;
            for (int i = 0; i < playerDatas.Count; i++)
            {
                if (playerDatas[i].m_PlayerID == PickPlayer)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = (currentIndex + 1) % playerDatas.Count;
            PickPlayer = playerDatas[nextIndex].m_PlayerID;
        }





        private bool CheckplayerDatas()
        {
            if (playerDatas == null || playerDatas.Count == 0) return false;

            foreach (var data in playerDatas) if (data.m_PlayerID != 0) return true;

            return false;

        }

        /// <summary> 注：创建玩家 </summary>
        public void CreatPlayer(string playerName)
		{
			if (playerDatas == null) playerDatas = new Array<PlayerData>();
			playerDatas.Add(new PlayerData() {m_Name = playerName });
		}

	}
}
