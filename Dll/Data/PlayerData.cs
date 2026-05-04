using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 维修公司.Dll.data;
using 途畔归所.Dll.Comp;

namespace 途畔归所.Dll.Data
{
	public partial class PlayerData : Resource
	{

		[Export] private string _Name;

		[Export] private int _playerID;

		[Export] public float m_Speed = 5.0f;

        [Export] public float m_Jump = 4.5f;

        [Export] public Godot.Collections.Dictionary<int, ItemData> m_InventoryData = [];


		public string m_Name { get => _Name; set { _Name = value; SetPlayerID(); } }

		public int m_PlayerID { get => _playerID; }

		private int SetPlayerID() => _playerID = (_playerID == default) ? Math.Abs(Guid.NewGuid().GetHashCode()) : _playerID;

		public void UpdateInventoryData(Array<SlotComp> slotComps)
		{
			m_InventoryData.Clear();

			foreach (var Slot in slotComps)
			{
				if (Slot == null || Slot.IsSlotEmpty) continue;
				m_InventoryData.Add(Slot.m_SlotID, Slot.m_ItemData.DeepCopy());
            }
		}

		public int GetInventoryItemCount()
		{
			if (m_InventoryData == null || m_InventoryData.Count == 0) return 0;
			return m_InventoryData.Count;
		}

		public PlayerData DeepCopy()
		{
			var data = this.DuplicateDeep() as PlayerData;
			if (data == null) return null;

			return data;
		}

	}
}
