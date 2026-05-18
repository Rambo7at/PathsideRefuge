using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using 维修公司.Dll.data;
using 途畔归所.Dll.Utils;
using 途畔归所.Dll.View;

/// <summary>注：负责管理库存相关功能，包括初始化库存格子、处理物品操作与存档交互等。</summary>
public partial class InventoryComp : Node
{
    public enum InventoryType
    {
        Backpack = 0,
        Chest = 1,
    }
    [Export] public int m_maxCol = 1;

    [Export] public int m_maxRow = 1;

    [Export] public InventoryType m_inventoryType;

    [Export] public Node3D m_dropPos;

    public int m_capacity => m_maxCol * m_maxRow;


    public Array<ItemData> m_SlotDatas = [];


    public Action OnChanged;

    public Action OnToggle;

    public Func<bool> Ui_Visible;


    /// <summary>注：初始化库存格子UI并加载存档数据，完成后刷新显示。</summary>
    public override void _Ready()
    {
        for (int i = 0; i < m_capacity; i++) m_SlotDatas.Add(null);
    }


    /// <summary>注：尝试添加物品到库存，若成功则刷新显示，物品为空时打印错误。</summary>
    public bool TryAddItem(ItemData itemData)
    {
        if (itemData == null)
        {
            CatLog.Warn("[InventoryComp.TryAddItem] 传入的ItemData为空，添加库存失败");
            return false;
        }

        if (TryStackItemInInventory(itemData))
        {
            OnChanged?.Invoke();
            return true;
        }

        var indx = FindEmptySlot();
        if (indx == -1) return false;

        m_SlotDatas[indx] = itemData.DeepCopy();
        OnChanged?.Invoke();
        return true;
    }

    public void SwapSlots(SlotView srcSlot, SlotView destSlot)
    {
        if (srcSlot.isNull) return;

        if (destSlot.isNull)
        {
            destSlot.m_slotData = srcSlot.m_slotData.DeepCopy();
            srcSlot.m_slotData = null;
        }
        else
        {
            var srcData = srcSlot.m_slotData.DeepCopy();
            var destData = destSlot.m_slotData.DeepCopy();

            destSlot.m_slotData = srcData;
            srcSlot.m_slotData = destData;

        }

        destSlot.Refresh();
        srcSlot.Refresh();
    }

    /// <summary>注：查询库存中的空格子。</summary>
    /// <returns>空的格子组件。</returns>
    private int FindEmptySlot()
    {
        for (int i = 0; i < m_SlotDatas.Count; i++)
        {
            if (m_SlotDatas[i] == null) return i;
        }
        return -1;
    }

    /// <summary>注：查找库存中可堆叠指定物品的格子，并尝试堆叠物品，返回是否成功堆叠完物品。</summary>
    private bool TryStackItemInInventory(ItemData itemData)
    {
        foreach (var slotdata in m_SlotDatas)
        {
            if (itemData.m_Stack <= 0) return true;
            if (slotdata != null && slotdata.m_ID == itemData.m_ID && slotdata.m_IsStackable) slotdata.TryStack(itemData);
        }
        return itemData.m_Stack < 1;
    }
}
