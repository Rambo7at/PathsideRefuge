using Godot;
using Godot.Collections;
using System.Collections.Generic;
namespace 途畔归所.Dll.Data
{

	public partial class SaveData : Resource
	{

		[Export] public Godot.Collections.Dictionary<int, PlayerData> m_PlyaerDataDict { get; set; } = [];


        [Export] public Godot.Collections.Dictionary<string, Variant> PlacedRuntimeData = [];

        [Export] public int PickPlayer = 0;







        /// <summary> 注：获取选择的玩家 </summary>
        /// <returns>玩家数据</returns>
        public PlayerData GetPickPlayerData()
        {
            if (!CheckPlyaerDataDict()) return null;

            if (m_PlyaerDataDict.TryGetValue(PickPlayer, out PlayerData player) && player != null) return player;

            foreach (var pldata in m_PlyaerDataDict) if (pldata.Value != null)
            {
                PickPlayer = pldata.Key;
                return pldata.Value;
            }

            return null;
        }

        /// <summary> 注：创建新玩家 </summary>
        public void CreatPlayer(string playerName)
		{
            if (m_PlyaerDataDict == null) return;

            PlayerData pldata = new PlayerData() { m_Name = playerName };

            m_PlyaerDataDict.Add(pldata.m_PlayerID, pldata);
		}


        /// <summary> 注：获取全部ID </summary>
        public List<int> GetAllPlayerID()
        {
            List<int> ids = [];

            if (m_PlyaerDataDict == null) return ids; 

          

            foreach (var pdata in m_PlyaerDataDict)
            {
                ids.Add(pdata.Key);
            }

            return ids;
        }

        private bool CheckPlyaerDataDict()
        {
            if (m_PlyaerDataDict == null || m_PlyaerDataDict.Count == 0) return false;
            return true;
        }


    }
}
