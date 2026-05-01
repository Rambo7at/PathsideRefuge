using Godot;
using Godot.Collections;
using 维修公司.Dll;
using 维修公司.Dll.data;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Comp;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;

public partial class InventoryComp : UIPanelBase
{
    [Export] private GridContainer m_GridContainer;

    private Player m_player;
    private PlayerData m_PlayerData;
    private Marker3D m_Marker3D;

    public Array<SlotComp> m_InventorySlots = new Array<SlotComp>();

    public override void _Ready()
    {
        if (m_player == null)
        {
            GD.Print("[InventoryComp._Ready()]:InventoryComp中未有传入 Player ");
            return;
        }

        m_Marker3D = m_player.m_eye;
        m_PlayerData = m_player.m_PlayerData;

        for (int i = 0; i < 10; i++)
        {
            SlotComp slotUi = UIManager.Instance.GetUI("slot_ui") as SlotComp;
            m_GridContainer.AddChild(slotUi);
            m_InventorySlots.Add(slotUi);
            slotUi.m_SlotID = i;
            slotUi.BindPlayer(m_player);
        }

        if (m_PlayerData.m_InventoryData == null || m_PlayerData.m_InventoryData.Count != 10) return;

        foreach (var data in m_PlayerData.m_InventoryData)
        {
            if (data.m_ItemData == null) continue;
            m_InventorySlots[data.m_SlotIndex].m_SlotData = data.DeepCopy();
        }
        RefSlot();
    }
    public override void _Process(double delta)
    {
    }

    /// <summary>注：绑定归属玩家。</summary>
    public void BindPlayer(Player player) => m_player = player;

    /// <summary>注：加入背包物品。</summary>
    /// <param name="itemDrop">物品实体。</param>
    /// <returns>是否添加成功。</returns>
    public bool AddItem(ItemComp itemDrop)
    {
        if (itemDrop == null)
        {
            GD.PrintErr("[InventoryComp.AddItem] 传入的ItemDrop节点为空");
            return false;
        }

        ItemData newItemData = itemDrop.m_ItemData;
        if (newItemData == null)
        {
            GD.PrintErr($"[InventoryComp.AddItem] 物品[{itemDrop.m_ItemData.m_Name}]创建ItemData失败");
            return false;
        }

        foreach (var cmop in m_InventorySlots) cmop.TryStack(newItemData);

        RefSlot();

        if (newItemData.m_Stack <= 0) return true;
        var slot = FindEmptySlot();
        RefSlot();
        if (slot == null) return false;
        slot.ApplyData(newItemData);
        RefSlot();
        return true;
    }

    /// <summary>注：刷新背包格子显示，并同步存档数据。</summary>
    public void RefSlot()
    {
        foreach (var comp in m_InventorySlots) comp.Refresh();
        m_PlayerData.UpdateInventoryData(m_InventorySlots);
    }

    /// <summary>注：查询背包中的空格子。</summary>
    /// <returns>空的格子组件。</returns>
    private SlotComp FindEmptySlot()
    {
        foreach (var comp in m_InventorySlots) if (comp.IsSlotNull()) return comp;
        return null;
    }

    /// <summary>注：检测背包是否还有空位。</summary>
    /// <returns>是/否。</returns>
    private bool IsInventoryEmpty()
    {
        foreach (var comp in m_InventorySlots) if (comp.IsSlotNull()) return true;
        return false;
    }

    public void ToggleUI() => this.Visible = !this.Visible;
}