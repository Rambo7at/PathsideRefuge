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
    public void SwapSlots(SlotComp SlotThis, SlotComp SlotTarget)
    {
        if (SlotThis == null || SlotTarget == null) return;

        if (SlotTarget.IsSlotEmpty)
        {
            SlotTarget.m_ItemData = SlotThis.m_ItemData.DeepCopy();

            SlotThis.m_ItemData = null;

            RefSlot();
        }
        else if(SlotTarget.m_ItemData == SlotThis.m_ItemData)
        {
            SlotTarget.m_ItemData.TryStack(SlotThis.m_ItemData);
            RefSlot();
        }
        else if (SlotTarget.m_ItemData != SlotThis.m_ItemData)
        {
            ItemData dataA = SlotThis.m_ItemData.DeepCopy();
            ItemData dataB = SlotTarget.m_ItemData.DeepCopy();
            SlotThis.m_ItemData = dataB;
            SlotTarget.m_ItemData = dataA;
            RefSlot();
        }
    }

    /// <summary>注：刷新背包格子显示，并同步存档数据。</summary>
    public void RefSlot()
    {
        foreach (var comp in m_InventorySlots) comp.Refresh();
        Holder.SaveInventory(m_InventorySlots);
    }


    public void Equip(ItemData itemData)
    {
        if (itemData == null) return;

        var pl = PlayerManager.Instance.m_LocalPlayer;

        if (pl == null)
        {
            GD.PrintErr("【[InventoryComp.Equip]：获取的玩家对象是空的");
            return;
        }

        pl.Equip(itemData);
    }










    /// <summary>注：查询背包中的空格子。</summary>
    /// <returns>空的格子组件。</returns>
    private SlotComp FindEmptySlot()
    {
        foreach (var comp in m_InventorySlots) if (comp.IsSlotEmpty) return comp;
        return null;
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

    [Obsolete("暂时没用")]
    private ItemData RemoveItem(int slotIndex)
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
}
