using Godot;
using 维修公司.Dll.data;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.View
{
	public partial class SlotView : Control
	{
		[Export] public Button m_button;

		[Export] public TextureRect m_itemIcon;

		[Export] public Label m_itemInfo;

		[Export] public CheckBox m_checkBox;  // 临时功能后续可能删除 

		public int m_slotIndex;

		public InventoryView m_ownerView;

		public ItemData m_slotData { get => m_ownerView.m_inventoryComp.m_SlotDatas[m_slotIndex]; set => m_ownerView.m_inventoryComp.m_SlotDatas[m_slotIndex] = value; }

		public bool isNull => m_slotData == null; 

		public override void _Ready()
		{
			if (m_button == null || m_itemIcon == null || m_itemInfo == null || m_checkBox == null)
			{
				CatLog.Err($"[SlotView._Ready]：检测需求字段 有空 已销毁");
				CatUtils.StopAndExit(this);
				return;
			}

			Refresh();

		}

		/// <summary>注：刷新格子的图标与数量显示。</summary>
		public void Refresh()
		{
			if (m_slotData == null)
			{
				m_itemInfo.Text = string.Empty;
				m_itemIcon.Texture = null;
				m_itemIcon.Visible = true;
			}
			else
			{
				m_itemInfo.Text = $"{m_slotData.m_Name} x{m_slotData.m_Stack}";
				m_itemIcon.Texture = m_slotData.m_Icon;
			}
		}

	}
}
