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
		[Export] private Array<SlotView> m_EquipSlots { get; set; } = [];

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
                if (m_EquipSlots[i].m_IsEquipSlot == false)
                {
                    CatUtils.StopAndExit(this);
                    return;
                }
                m_EquipSlots[i].m_slotIndex = i;
            }

            if (GetParent() is not IEquipmentHolder holder)
            {
                CatLog.Err($"[InventoryView._Ready]：父对象没有 IInventoryHolder 接口，已销毁");
                CatUtils.StopAndExit(this);
                return;
            }


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
                    EquipData.Remove(EquipData[i]);
                }
            }


            for (int i = 0; i < EquipData.Count; i++)
            {
                if (EquipData[i].Type != m_EquipSlots[i].m_EquipType)
                {
                    EquipData[i]?.TryDropItem(m_Holder.DropPos);
                    EquipData[i] = null;
                }
            }

            foreach (var item in m_EquipSlots)
            {
                item.m_holder = EquipData;
            }



        }


	}
}
