using Godot;
using Godot.Collections;
using 维修公司.Dll.data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Utils;
using static 维修公司.Dll.data.ItemData;

namespace 途畔归所.Dll.View
{
	public partial class EquipmentView : Control
	{
		[Export] private Array<SlotView> m_EquipSlots { get; set; } 

		private IEquipmentHolder m_Holder { get; set; }

		public override void _EnterTree()
		{
			if (m_EquipSlots == null || m_EquipSlots.Count == 0)
			{
				CatUtils.StopAndExit(this);
				return;
			}

			for (int i = 0; i < m_EquipSlots.Count; i++)
			{
				if (!m_EquipSlots[i].m_IsEquipSlot)
				{
					CatLog.Warn($"[EquipmentView._EnterTree] m_EquipSlots[{i}] 的 m_IsEquipSlot 为 false，已自动修正");
					m_EquipSlots[i].m_IsEquipSlot = true;
				}
			}

			if (GetParent() is not IEquipmentHolder holder)
			{
				CatLog.Err($"[EquipmentView._EnterTree]：父对象没有 IEquipmentHolder 接口，已销毁");
				CatUtils.StopAndExit(this);
				return;
			}
			CatLog.Debug($"m_EquipSlots 数量: {m_EquipSlots.Count}");

			m_Holder = holder;
			Array<ItemData> EquipData = m_Holder.EquipData;

			while (EquipData.Count < m_EquipSlots.Count)
			{
				EquipData.Add(null);
			}

			if (EquipData.Count > m_EquipSlots.Count)
			{
				for (int i = EquipData.Count - 1; i >= m_EquipSlots.Count; i--)
				{
					EquipData[i]?.TryDropItem(m_Holder.DropPos);
					EquipData.RemoveAt(i);
				}
			}

			for (int i = 0; i < EquipData.Count; i++)
			{
				if (EquipData[i] != null && EquipData[i].Type != m_EquipSlots[i].m_EquipType)
				{
					EquipData[i]?.TryDropItem(m_Holder.DropPos);
					EquipData[i] = null;
				}

				int index = i;

				m_EquipSlots[i].OnDropPos = () => m_Holder.DropPos;
				m_EquipSlots[i].OnGetItem += () => EquipData[index];
				m_EquipSlots[i].OnSetItem += (newItemData) => EquipData[index] = newItemData;
			}

		}


		public void ToggleUI()
		{
			Visible = !Visible;
			if (Visible) RefreshAllSlots();
		}

		public void RefreshAllSlots() { foreach (var slot in m_EquipSlots) slot.Refresh(); }
	}
}
