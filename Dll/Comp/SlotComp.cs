using Godot;
using System;
using 维修公司.Dll.data;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;

namespace 途畔归所.Dll.Comp
{
	public partial class SlotComp : UIPanelBase
	{
		[Export] public Button m_button;
		[Export] public TextureRect m_icon;
		[Export] public Label m_text;

		private Player m_Player;

		public int m_SlotID { get => m_SlotData.m_SlotIndex; set => m_SlotData.m_SlotIndex = value; }
		public SlotData m_SlotData;

		private bool isDragging = false;
		private TextureRect dragIcon;

		/// <summary>注：初始化格子数据</summary>
		public override void _Ready() => m_SlotData = (m_SlotData == null) ? m_SlotData = new SlotData() : m_SlotData;

		/// <summary>
		/// 回调函数：GUI_Input
		/// </summary>
		/// <param name="mInput"></param>
		public void OnGuiInput(InputEvent mInput)
		{
			// 鼠标抬起
			if (mInput is InputEventMouseButton btn)
			{
				OnClickUp(btn);
				return;
			}

			// 鼠标移动
			if (mInput is InputEventMouseMotion motion)
			{
				DoDrag(motion);
			}
		}

		/// <summary> 注：拖动功能</summary>
		/// <param name="motion"></param>
		private void DoDrag(InputEventMouseMotion motion)
		{
			if (isDragging && dragIcon != null)
			{
				dragIcon.GlobalPosition = motion.GlobalPosition;
				return;
			}

			if (motion.ButtonMask != MouseButtonMask.Left) return;  // 不按住拖动 返回

			if (m_SlotData == null || m_SlotData.m_ItemData == null) return; // 格子没有数据 返回

			if (dragIcon != null) return;

			isDragging = true;
			m_icon.Visible = false;

			dragIcon = new TextureRect();
			dragIcon.ExpandMode = m_icon.ExpandMode;
			dragIcon.Size = m_icon.Size;
			dragIcon.Texture = m_icon.Texture;
			dragIcon.ZIndex = 1000;
			dragIcon.TopLevel = true;
			dragIcon.MouseFilter = MouseFilterEnum.Ignore;
			m_Player.m_CanvasLayer.AddChild(dragIcon);
		}

		/// <summary> 注：停止拖动 </summary>
		/// <param name="btn"></param>
		private void OnClickUp(InputEventMouseButton btn)
		{
			if (btn.ButtonIndex == MouseButton.Left && !btn.Pressed)
			{
				dragIcon?.QueueFree();
				dragIcon = null;
				isDragging = false;
				m_icon.Visible = true;
				SwapSlot();
			}
		}

		/// <summary>注：交换格子物品</summary>
		private void SwapSlot()
		{
			var comp = GetMouseSlot();
			if (comp == null)
			{
				DropItem();
                return;
            }



			if (comp.m_SlotID == m_SlotID) return;

			if (comp.m_SlotData.m_ItemData == null)
			{
				comp.m_SlotData.m_ItemData = m_SlotData.m_ItemData.CopyData();
				m_SlotData.m_ItemData = null;
			}
			else
			{
				var data = comp.m_SlotData.m_ItemData.CopyData();
				comp.m_SlotData.m_ItemData = m_SlotData.m_ItemData.CopyData();
				m_SlotData.m_ItemData = data;
			}
			comp.Refresh();
			Refresh();
		}

        /// <summary>注：丢弃物品</summary>
        private void DropItem()
        {
            if (m_SlotData == null || m_SlotData.IsSlotNull) return;

            ItemData dropData = m_SlotData.m_ItemData.CopyData();

            RigidBody3D drop = dropData.DataToDrop();
            if (drop == null) return;

            SceneTree tree = GetTree();
            if (tree?.CurrentScene == null)
            {
                drop.QueueFree();
                return;
            }
            tree.CurrentScene.AddChild(drop);

            drop.GlobalPosition = m_Player.m_eye.GlobalPosition + m_Player.m_eye.GlobalBasis.Z * -1.0f;

            m_SlotData.m_ItemData = null;
            Refresh();

            m_Player.m_InventoryComp?.RefSlot();
        }

        /// <summary>注：获取鼠标指向的格子</summary>
        private SlotComp GetMouseSlot()
		{
			Control control = GetViewport().GuiGetHoveredControl();
			if (control == null) return null;
			var comp = control.GetParent();
			if (comp == null) return null;
			return comp as SlotComp;
		}

		/// <summary>注：判断格子是否为空</summary>
		public bool IsSlotNull() => m_SlotData.IsSlotNull;

		/// <summary>注：尝试堆叠物品</summary>
		/// <param name="itemData">待堆叠物品数据</param>
		public void TryStack(ItemData itemData)
		{
			if (m_SlotData == null || m_SlotData.m_ItemData == null) return;

			if (m_SlotData.m_ItemData.m_ID == itemData.m_ID && m_SlotData.m_ItemData.IsStack())
			{
				m_SlotData.m_ItemData.TryStack(itemData);
			}
		}

		/// <summary>注：应用物品数据到格子</summary>
		/// <param name="itemData">物品数据</param>
		public void ApplyData(ItemData itemData)
		{
			if (IsSlotNull()) m_SlotData.m_ItemData = itemData;
		}

		/// <summary>注：刷新格子显示内容 </summary>
		public void Refresh()
		{
			if (IsSlotNull())
			{
				m_text.Text = string.Empty;
				m_icon.Texture = null;
				m_icon.Visible = true;
			}
			else
			{
				m_text.Text = $"{m_SlotData.m_ItemData.m_Name} x{m_SlotData.m_ItemData.m_Stack}";
				m_icon.Texture = m_SlotData.m_ItemData.m_Icon;
			}
		}

		public void BindPlayer(Player player) => m_Player = player;
	}
}
