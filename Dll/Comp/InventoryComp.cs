using Godot;
using Godot.Collections;
using 维修公司.Dll;
using 维修公司.Dll.data;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Comp;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;

/// <summary>注：背包组件 </summary>
public partial class InventoryComp : UIPanelBase
{
	[Export] private GridContainer gridContainer;

	private Player m_player;
	private PlayerData m_PlayerData;
	private Marker3D m_Marker3D;

	/// <summary>注：背包格子数据 </summary>
	public Array<SlotComp> m_inventorySlots = new Array<SlotComp>();

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
			gridContainer.AddChild(slotUi);
			m_inventorySlots.Add(slotUi);
			slotUi.m_SlotID = i;
			slotUi.BindPlayer(m_player);

		}

		if (m_PlayerData.m_InventoryData == null && m_PlayerData.m_InventoryData.Count != 10) return;

		foreach (var data in m_PlayerData.m_InventoryData)
		{
			if (data.m_ItemData == null) return;
			m_inventorySlots[data.m_SlotIndex].m_SlotData = data.CopyData();
		}

	}

	public override void _Process(double delta)
	{

	}

	/// <summary>注：绑定归属玩家 </summary>
	public void BindPlayer(Player player) => m_player = player;


	/// <summary>注：加入背包物品</summary>
	/// <param name="itemDrop">物品实体</param>
	/// <returns>是否添加成功</returns>
	public bool AddItem(ItemComp itemDrop)
	{
		if (itemDrop == null)
		{
			GD.PrintErr("[InventoryComp.AddItem] 传入的ItemDrop节点为空");
			return false;
		}

		ItemData newItemData = itemDrop.CreateItemData();
		if (newItemData == null)
		{
			GD.PrintErr($"[InventoryComp.AddItem] 物品[{itemDrop.名称}]创建ItemData失败");
			return false;
		}

		foreach (var cmop in m_inventorySlots) cmop.TryStack(newItemData);

		RefSlot();


		if (newItemData.m_Stack <= 0) return true;
		var slot = FindEmptySlot();
		RefSlot();
		if (slot == null) return false;
		slot.ApplyData(newItemData);
		RefSlot();
		return true;
	}



	/// <summary>工具方法：刷新背包格子显示</summary>
	public void RefSlot()
	{
		foreach (var comp in m_inventorySlots) comp.Refresh();
	}

	/// <summary>工具方法：查询背包中的空格子</summary>
	/// <returns>空的格子组件</returns>
	private SlotComp FindEmptySlot()
	{
		foreach (var comp in m_inventorySlots) if (comp.IsSlotNull()) return comp;
		return null;
	}

	/// <summary>工具方法：检测背包是否还有空位</summary>
	/// <returns>是/否</returns>
	private bool IsInventoryEmpty()
	{
		foreach (var comp in m_inventorySlots) if (comp.IsSlotNull()) return true;
		return false;
	}

	/// <summary>注：显示与隐藏 </summary>
	public void ToggleUI() => this.Visible = !this.Visible;

}
