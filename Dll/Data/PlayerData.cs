using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Comp;

namespace 途畔归所.Dll.Data
{

	public partial class PlayerData : Resource
	{

		[Export] private string _Name;

		[Export] private int _playerID;

		[Export] public Array<SlotData> m_InventoryData = new Array<SlotData>();

		public Player m_LocalPlayer;


		public string m_Name { get => _Name; set { _Name = value; SetPlayerID();} }

		public int m_PlayerID { get => _playerID; }

		private int SetPlayerID() => _playerID = (_playerID == default) ? Math.Abs(Guid.NewGuid().GetHashCode()) : _playerID;

		public int CheckPlayerInventoryCount()
		{
			if (m_InventoryData == null || m_InventoryData.Count == 0) return 0;

			int count = 0;
			foreach (var item in m_InventoryData)
			{
				if (!item.IsSlotNull)
				{
					GD.Print("背包的物品是"+ item.m_ItemData.m_Name);
					GD.Print("它的格子标号是" + item.m_SlotIndex);
					count++;
				}
			}

			return count;

		}







	}
}
