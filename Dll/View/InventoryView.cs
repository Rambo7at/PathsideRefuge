using Godot;
using Godot.Collections;
using 维修公司.Dll.data;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.View
{
	public partial class InventoryView : Control
	{
		[Export] private GridContainer m_gridContainer;
		[Export] private string m_uiName;

		public InventoryComp m_inventoryComp;
		private Array<SlotView> m_slotViewArr = [];

		private bool m_IsDragging = false;
		private TextureRect m_DragIcon;
		private SlotView m_DragSourceSlot;   

		public override void _Ready()
		{
			if (m_inventoryComp == null)
			{
				CatLog.Err("[InventoryView._Ready]：检测 InventoryView 的数据层是 空的");
				CatUtils.StopAndExit(this);
				return;
			}

			m_inventoryComp.OnChanged += RefreshAllSlots;
			m_inventoryComp.OnToggle += ToggleUI;
			m_inventoryComp.Ui_Visible += () => Visible;

			for (int i = 0; i < m_inventoryComp.m_capacity; i++)
			{
				var ui = UIManager.Instance.GetUI(m_uiName);
				if (ui is not SlotView view)
				{
					CatLog.Err("[InventoryView._Ready] 格子UI类型错误");
					CatUtils.StopAndExit(this);
					return;
				}
				view.m_slotIndex = i;
				view.m_ownerView = this;
				view.m_button.GuiInput += OnInventoryGuiInput;
				m_slotViewArr.Add(view);
				m_gridContainer.AddChild(view);
			}

			RefreshAllSlots();
		}

		public void BindData(InventoryComp inventoryComp) => m_inventoryComp ??= inventoryComp;

		public void RefreshAllSlots() { foreach (var slot in m_slotViewArr) slot.Refresh(); }

		public void ToggleUI() => Visible = !Visible;

		// 事件入口：只处理鼠标按键，移动已在 DoDrag 中单独处理
		public void OnInventoryGuiInput(InputEvent input)
		{
			if (input is InputEventMouseButton btn)
			{
				if (btn.ButtonIndex == MouseButton.Left)
				{
					if (btn.Pressed)
					{
						// 按下左键：开始拖拽（延迟到移动事件中实际创建图标，这里可留空）
					}
					else
					{
						// 释放左键：停止拖拽
						StopDrag();
					}
				}
			}
			else if (input is InputEventMouseMotion motion)
			{
				DoDrag(motion);
			}
		}

		/// <summary>执行物品拖动逻辑</summary>
		private void DoDrag(InputEventMouseMotion motion)
		{
			// 已经在拖拽中：只更新图标位置
			if (m_IsDragging && m_DragIcon != null)
			{
				m_DragIcon.GlobalPosition = motion.GlobalPosition;
				return;
			}

			// 左键未按下，不开始拖拽
			if (motion.ButtonMask != MouseButtonMask.Left) return;

			// 获取当前鼠标下的格子作为源格子
			SlotView sourceSlot = GetMouseSlot();
			if (sourceSlot == null || sourceSlot.m_slotData == null) return;

			// 开始拖拽
			m_IsDragging = true;
			m_DragSourceSlot = sourceSlot;

			// 隐藏源格子图标
			sourceSlot.m_itemIcon.Visible = false;

			// 创建拖拽图标
			m_DragIcon = new TextureRect()
			{
				ExpandMode = sourceSlot.m_itemIcon.ExpandMode,
				Size = sourceSlot.m_itemIcon.Size,
				Texture = sourceSlot.m_itemIcon.Texture,
				ZIndex = 1000,
				TopLevel = true,
				MouseFilter = MouseFilterEnum.Ignore,
				GlobalPosition = motion.GlobalPosition
			};
			PlayerManager.Instance.m_CanvasLayer.AddChild(m_DragIcon);
		}

		/// <summary>停止拖拽，执行交换或丢弃</summary>
		private void StopDrag()
		{
			if (!m_IsDragging) return;

			// 清理拖拽图标
			m_DragIcon?.QueueFree();
			m_DragIcon = null;
			m_IsDragging = false;

			// 恢复源格子图标
			if (m_DragSourceSlot == null) return;
			m_DragSourceSlot.m_itemIcon.Visible = true;

			// 获取释放时鼠标下的目标格子（全局查找）
			SlotView t_Slot = GetMouseSlot();

			if (t_Slot == null)
			{
				m_inventoryComp.DropItem(m_DragSourceSlot);
			}
			else 
			{
				m_inventoryComp.SwapSlots(m_DragSourceSlot, t_Slot);
			}
			m_DragSourceSlot = null;
		}



		/// <summary>获取当前鼠标悬停位置所在的 SlotView</summary>
		private SlotView GetMouseSlot()
		{
			Control hovered = GetViewport().GuiGetHoveredControl();
			while (hovered != null)
			{
				if (hovered is SlotView slot)
					return slot;
				hovered = hovered.GetParent() as Control;
			}
			return null;
		}
	}
}
