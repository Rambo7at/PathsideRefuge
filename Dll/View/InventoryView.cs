using Godot;
using Godot.Collections;
using 维修公司.Dll.data;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.View
{
    public partial class InventoryView : Control
    {
        [Export] private GridContainer m_gridContainer;

        private IInventoryHolder m_holder;

        private Array<SlotView> m_slotViewArr = [];

        public override void _Ready()
        {
            if (GetParent() is not IInventoryHolder holder)
            {
                CatLog.Err($"[InventoryView._Ready]：父对象没有 IInventoryHolder 接口，已销毁");
                CatUtils.StopAndExit(this);
                return;
            }

            m_holder = holder;

            int dataConut = m_holder.InventoryData.m_capacity;
            InventoryData inventoryData = m_holder.InventoryData;
            Array<ItemData> slotDataArr = m_holder.InventoryData.m_itemArr;

            while (slotDataArr.Count < dataConut)
            {
                slotDataArr.Add(null);
            }

            if (slotDataArr.Count > dataConut)
            {
                for (int i = slotDataArr.Count - 1; i >= dataConut; i--)
                {
                    slotDataArr[i]?.TryDropItem(m_holder.DropPos);
                    slotDataArr.Remove(slotDataArr[i]);
                }
            }

            for (int i = 0; i < slotDataArr.Count; i++)
            {
                if (UIManager.Instance.GetUI(inventoryData.m_SlotUIName) is not SlotView view)
                {
                    CatLog.Err("[InventoryView._Ready] 格子UI类型错误");
                    CatUtils.StopAndExit(this);
                    return;
                }
                view.m_slotIndex = i;
                view.m_holder = m_holder;
                m_slotViewArr.Add(view);
                m_gridContainer.AddChild(view);
            }

            RefreshAllSlots();
            inventoryData.OnChanged += RefreshAllSlots;
        }

        public void RefreshAllSlots() { foreach (var slot in m_slotViewArr) slot.Refresh(); }

        public void ToggleUI()
        {
            Visible = !Visible;
            if (Visible) RefreshAllSlots();
        }

    }

}
