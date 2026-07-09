using Godot;
using Godot.Collections;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Utils;
using static 维修公司.Dll.data.ItemData;

namespace 途畔归所.Dll.View
{
	public partial class EquipmentView : Control
	{
		[Export] private Array<SlotView> m_EquipSlots { get; set; }

		private IEquipmentHolder m_Holder { get; set; }

		private SlotView m_WeaponSlot;

		public override void _EnterTree()
		{
			CatLog.Ok($"[EquipmentView._EnterTree]：开始执行");

			if (m_EquipSlots == null || m_EquipSlots.Count == 0)
			{
				CatUtils.StopAndExit(this);
				CatLog.Err($"[EquipmentView._EnterTree]：装备栏未有添加 对应的 SlotView，已销毁");
				return;
			}

			foreach (var slot in m_EquipSlots)
			{
				if (slot == null)
				{
					CatLog.Warn($"[EquipmentView._EnterTree]：装备栏中有空的 SlotView 请检查编辑器");
					continue;
				}

				if (slot.m_IsEquipSlot == false) slot.m_IsEquipSlot = true;
				if (slot.m_EquipType == E_ItemType.Weapon) m_WeaponSlot = slot;
			}

			if (GetParent() is not IEquipmentHolder holder)
			{
				CatLog.Err($"[EquipmentView._EnterTree]：父对象没有 IEquipmentHolder 接口，已销毁");
				CatUtils.StopAndExit(this);
				return;
			}

			if (m_WeaponSlot == null)
			{
				CatLog.Err($"[EquipmentView._EnterTree]：装备栏未有添加 对应武器的 SlotView，已销毁");
				CatUtils.StopAndExit(this);
				return;
			}
			m_Holder = holder;

			m_WeaponSlot.OnDropPos += () => m_Holder.DropPos;
			m_WeaponSlot.OnGetItem += () => m_Holder.Equipment.m_WeaponData;
			m_WeaponSlot.OnSetItem += (newdata) => m_Holder.Equipment.m_WeaponData = newdata;
		}

		public void ToggleUI()
		{
			Visible = !Visible;
			if (Visible) RefreshAllSlots();
		}

		public void RefreshAllSlots() { foreach (var slot in m_EquipSlots) slot.Refresh(); }
	}

}
