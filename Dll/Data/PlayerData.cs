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


		public string m_Name { get => _Name; set { _Name = value; SetPlayerID(); } }

		public int m_PlayerID { get => _playerID; }

		private int SetPlayerID() => _playerID = (_playerID == default) ? Math.Abs(Guid.NewGuid().GetHashCode()) : _playerID;

		public void UpdateInventoryData(Array<SlotComp> slotComps)
		{
			m_InventoryData.Clear();

			foreach (var item in slotComps)
			{
				m_InventoryData.Add(item.m_SlotData.DeepCopy());
			}

		}

		public int GetInventoryItemCount()
		{
			if (m_InventoryData == null || m_InventoryData.Count == 0) return 0;

			int index = 0;

			foreach (var item in m_InventoryData)
			{
				if (item.IsSlotNull) continue;
				index++;
			}
			return index;
		}

		public PlayerData DeepCopy()
		{
			var data = this.DuplicateDeep() as PlayerData;
			if (data == null) return null;

			return data;
		}

	}
}
