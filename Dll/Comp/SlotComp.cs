using Godot;
using 维修公司.Dll.data;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using static Godot.Control;

public partial class SlotComp : UIPanelBase
{



	[Export] public Button m_button;
	[Export] public TextureRect m_icon;
	[Export] public Label m_text;
	public InventoryComp m_OwnerInventory { get; set; }
	public bool IsSlotEmpty { get => m_ItemData == null; }

	public int m_SlotID;
	public ItemData m_ItemData;

	private bool m_IsDragging = false;
	private TextureRect m_DragIcon;

	public override void _Ready() { }

	/// <summary>注：处理格子上的 GUI 输入事件（鼠标按下/移动）。</summary>
	/// <param name="mInput">输入事件对象。</param>
	public void OnGuiInput(InputEvent mInput)
	{
		if (mInput is InputEventMouseButton btn)
		{
			OnClickUp(btn);
			return;
		}

		if (mInput is InputEventMouseMotion motion)
		{
			DoDrag(motion);
		}
	}

	/// <summary>注：执行物品拖动逻辑。</summary>
	/// <param name="motion">鼠标移动事件。</param>
	private void DoDrag(InputEventMouseMotion motion)
	{
		if (m_IsDragging && m_DragIcon != null)
		{
			m_DragIcon.GlobalPosition = motion.GlobalPosition;
			return;
		}

		if (motion.ButtonMask != MouseButtonMask.Left) return;

		if (m_ItemData == null) return;

		if (m_DragIcon != null) return;

		m_IsDragging = true;
		m_icon.Visible = false;

		m_DragIcon = new TextureRect();
		m_DragIcon.ExpandMode = m_icon.ExpandMode;
		m_DragIcon.Size = m_icon.Size;
		m_DragIcon.Texture = m_icon.Texture;
		m_DragIcon.ZIndex = 1000;
		m_DragIcon.TopLevel = true;
		m_DragIcon.MouseFilter = MouseFilterEnum.Ignore;
		m_OwnerInventory?.Holder?.GetCanvasLayer()?.AddChild(m_DragIcon);
	}

	/// <summary>注：鼠标左键抬起时停止拖动并执行交换或丢弃。</summary>
	/// <param name="btn">鼠标按钮事件。</param>
	private void OnClickUp(InputEventMouseButton btn)
	{
		if (btn.ButtonIndex == MouseButton.Left && !btn.Pressed)
		{
			m_DragIcon?.QueueFree();
			m_DragIcon = null;
			m_IsDragging = false;
			m_icon.Visible = true;
			SwapSlot();
		}
	}

	private void SwapSlot()
	{
		SlotComp targetSlot = GetMouseSlot();
		if (targetSlot == null || targetSlot == this)
		{
			if (targetSlot == null) DropItem();
			return;
		}

		if (m_OwnerInventory == null || targetSlot.m_OwnerInventory == null) return;


		// 同库存 → 交给库存处理
		if (m_OwnerInventory == targetSlot.m_OwnerInventory)
		{
			m_OwnerInventory.SwapSlots(m_SlotID, targetSlot.m_SlotID);
			return;
		}

		// 跨库存 → 先移除自己的物品
		ItemData movingItem = m_OwnerInventory.RemoveItem(m_SlotID);
		if (movingItem == null) return;

		// 尝试放入目标库存（直接操作引用）
		bool success = targetSlot.m_OwnerInventory.TryAddItemDirect(movingItem, targetSlot.m_SlotID);
		if (!success)
			m_OwnerInventory.TryAddItemDirect(movingItem); // 回退也用Direct

		m_OwnerInventory.RefSlot();
		targetSlot.m_OwnerInventory.RefSlot();
	}

	/// <summary>注：在当前鼠标没有指向任何格子时丢弃物品到世界。</summary>
	private void DropItem()
	{
		if (m_ItemData == null) return;

		// 通过接口询问是否允许丢弃
		IInventoryHolder holder = m_OwnerInventory?.Holder;
		if (holder == null ) return;	


		ItemData dropData = m_ItemData.DeepCopy();
		RigidBody3D drop = dropData.DataToDrop();
		if (drop == null) return;

		SceneTree tree = GetTree();
		if (tree?.CurrentScene == null)
		{
			drop.QueueFree();
			return;
		}
		tree.CurrentScene.AddChild(drop);

		// 从接口拿位置，不再碰 m_Player
		drop.GlobalPosition = holder.GetDropPosition();

		m_ItemData = null;
		Refresh();
		m_OwnerInventory?.RefSlot();
	}

	/// <summary>注：获取当前鼠标悬停位置所在的格子组件。</summary>
	/// <returns>鼠标下的 SlotComp，若没有则返回 null。</returns>
	private SlotComp GetMouseSlot()
	{
		Control control = GetViewport().GuiGetHoveredControl();
		if (control == null) return null;
		var comp = control.GetParent();
		if (comp == null) return null;
		return comp as SlotComp;
	}


	/// <summary>注：尝试将传入的物品数据堆叠到当前格子上。</summary>
	/// <param name="itemData">待堆叠的物品数据。</param>
	public void TryStack(ItemData itemData)
	{
		if (m_ItemData == null) return;
		m_ItemData.TryStack(itemData);
	}

	/// <summary>注：将物品数据直接填入当前空格子。</summary>
	/// <param name="itemData">要放入的物品数据。</param>
	public void ApplyData(ItemData itemData) => m_ItemData = itemData;

	/// <summary>注：刷新格子的图标与数量显示。</summary>
	public void Refresh()
	{
		if (IsSlotEmpty)
		{
			m_text.Text = string.Empty;
			m_icon.Texture = null;
			m_icon.Visible = true;
		}
		else
		{
			m_text.Text = $"{m_ItemData.m_Name} x{m_ItemData.m_Stack}";
			m_icon.Texture = m_ItemData.m_Icon;
		}
	}

	/// <summary>注：绑定所属玩家实例。</summary>
	public void BindHolder(InventoryComp Inventory) => m_OwnerInventory = Inventory;

	public SlotComp CopySlot()
	{
		return new SlotComp()
		{
			m_SlotID = this.m_SlotID,
			m_ItemData = this.m_ItemData.DeepCopy()
		};
	}
}
