using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using 维修公司.Dll.data;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;

public partial class InventoryComp : UIPanelBase
{
    [Export] private GridContainer m_GridContainer;

    public Array<SlotComp> m_InventorySlots = [];
    public int Capacity { get; set; } = 10;
    public IInventoryHolder Holder { get; set; }

    public event Action OnInventoryChanged;

    public override void _Ready()
    {

        for (int i = 0; i < Capacity; i++)
        {
            SlotComp slotUi = UIManager.Instance.GetUI("slot_ui") as SlotComp;
            m_GridContainer.AddChild(slotUi);
            m_InventorySlots.Add(slotUi);
            slotUi.m_SlotID = i;
            slotUi.BindHolder(this);
        }

        var data = LoadData(Holder.LoadInventory());
        if (data.Count == 0) RefSlot();
        else RefSlot();
    }

    public override void _Process(double delta)
    {
    }

    public bool TryAddItem(ItemData itemData)
    {
        if (itemData == null)
        {
            GD.PrintErr("[InventoryComp.TryAddItem] 传入的ItemData为空");
            return false;
        }



        foreach (var cmop in FindStackableSlot(itemData)) cmop.TryStack(itemData);
        RefSlot();

        if (itemData.m_Stack <= 0) return true;

        var slot = FindEmptySlot();
        if (slot == null) return false;

        slot.ApplyData(itemData);
        RefSlot();

        return true;
    }

    /// <summary>直接尝试放入物品（不拷贝，会修改 itemData.m_Stack）。跨库存专用。</summary>
    public bool TryAddItemDirect(ItemData itemData, int preferredSlot = -1)
    {
        if (itemData == null || itemData.m_Stack <= 0) return false;

        // 1. 尝试指定格子
        if (preferredSlot >= 0 && preferredSlot < m_InventorySlots.Count)
        {
            SlotComp slot = m_InventorySlots[preferredSlot];
            if (slot.IsSlotEmpty)
            {
                slot.m_ItemData = itemData;
                itemData.m_Stack = 0;
                slot.Refresh();
                OnInventoryChanged?.Invoke();
                return true;
            }
            else
            {
                slot.TryStack(itemData);
                if (itemData.m_Stack <= 0)
                {
                    slot.Refresh();
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        // 2. 尝试与其他格子堆叠
        foreach (var slot in m_InventorySlots)
        {
            if (slot.IsSlotEmpty) continue;
            slot.TryStack(itemData);
            if (itemData.m_Stack <= 0)
            {
                slot.Refresh();
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        // 3. 放入空位
        SlotComp empty = FindEmptySlot();
        if (empty != null)
        {
            empty.m_ItemData = itemData;
            itemData.m_Stack = 0;
            empty.Refresh();
            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }
    public ItemData RemoveItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= m_InventorySlots.Count) return null;
        SlotComp slot = m_InventorySlots[slotIndex];
        if (slot.IsSlotEmpty) return null;

        ItemData removed = slot.m_ItemData; // 直接交出引用（注意：不再拷贝）
        slot.m_ItemData = null;
        slot.Refresh();
        OnInventoryChanged?.Invoke();
        return removed;
    }


    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= m_InventorySlots.Count) return;
        if (indexB < 0 || indexB >= m_InventorySlots.Count) return;
        if (indexA == indexB) return;



        SlotComp slotA = m_InventorySlots[indexA];
        SlotComp slotB = m_InventorySlots[indexB];

        ItemData temp = slotA.m_ItemData;
        slotA.m_ItemData = slotB.m_ItemData;
        slotB.m_ItemData = temp;

        RefSlot();
    }

    /// <summary>注：刷新背包格子显示，并同步存档数据。</summary>
    public void RefSlot()
    {
        foreach (var comp in m_InventorySlots) comp.Refresh();
        Holder.SaveInventory(m_InventorySlots);
    }

    /// <summary>注：查询背包中的空格子。</summary>
    /// <returns>空的格子组件。</returns>
    private SlotComp FindEmptySlot()
    {
        foreach (var comp in m_InventorySlots) if (comp.IsSlotEmpty) return comp;
        return null;
    }

    /// <summary>注：检测背包是否还有空位。</summary>
    /// <returns>是/否。</returns>
    private bool IsInventoryEmpty()
    {
        foreach (var comp in m_InventorySlots) if (comp.IsSlotEmpty) return true;
        return false;
    }

    private Array<SlotComp> FindStackableSlot(ItemData itemData)
    {
        Array<SlotComp> slotArr = [];

        foreach (var slot in m_InventorySlots)
        {
            if (slot.m_ItemData != null && slot.m_ItemData.m_ID == itemData.m_ID && slot.m_ItemData.m_IsStackable)
            {
                slotArr.Add(slot);
            }
        }
        return slotArr;
    }

    private SlotComp FindSlotBySlotId(int slotId)
    {
        foreach (var slot in m_InventorySlots)
        {
            if (slot.m_SlotID == slotId)
            {
                return slot;
            }
        }
        return null;
    }

    private List<int> LoadData(Godot.Collections.Dictionary<int, ItemData> data)
    {
        if (data.Count == 0) return [];
        List<int> index = [];
        int slotCount = Capacity - 1;

        foreach (var slot in data)
        {
            if (slot.Key > slotCount)
            {
                index.Add(slot.Key);
                continue;
            }

            var ss = FindSlotBySlotId(slot.Key);
            if (ss == null) continue;

            ss.m_ItemData = slot.Value.DeepCopy();
        }
        return index;
    }

    public void ToggleUI() => this.Visible = !this.Visible;
}
