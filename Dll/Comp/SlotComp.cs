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

		public int m_SlotID { get => m_SlotData.m_SlotIndex; set => m_SlotData.m_SlotIndex = value; }
		public SlotData m_SlotData;

		private bool isDragging = false;
		private TextureRect dragIcon;
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

		private void DoDrag(InputEventMouseMotion motion)
		{
			if (isDragging && dragIcon != null)
			{
				dragIcon.GlobalPosition = motion.GlobalPosition;
				return;
			}

			if (motion.ButtonMask != MouseButtonMask.Left) return;

			if (m_SlotData == null || m_SlotData.m_ItemData == null) return;

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
			GameCore.Instance.m_PlayerManager.m_LocalPlayer.m_CanvasLayer.AddChild(dragIcon);
		}

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

		private void SwapSlot()
		{

			var comp = GetMouseSlot();
			if (comp == null) return;
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

		private SlotComp GetMouseSlot()
		{
			Control control = GetViewport().GuiGetHoveredControl();
			if (control == null) return null;
			var comp = control.GetParent();
			if (comp == null) return null;
			return comp as SlotComp;
		}

		public bool IsSlotNull() => m_SlotData.IsSlotNull;

		public void TryStack(ItemData itemData)
		{
			if (m_SlotData == null || m_SlotData.m_ItemData == null) return;

			if (m_SlotData.m_ItemData.m_ID == itemData.m_ID && m_SlotData.m_ItemData.IsStack())
			{
				m_SlotData.m_ItemData.TryStack(itemData);
			}
		}

		public void ApplyData(ItemData itemData)
		{
			if (IsSlotNull()) m_SlotData.m_ItemData = itemData;

		}

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


	}
}
