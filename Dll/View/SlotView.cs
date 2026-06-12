using Godot;
using 维修公司.Dll.data;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.View
{
	public partial class SlotView : Control
	{
        [ExportGroup("基础")]
        [Export] public Button m_button;
		[Export] public TextureRect m_itemIcon;
		[Export] public Label m_itemInfo;


		public int m_slotIndex { get; set; }
		public InventoryComp m_owner { get; set; }
        public ItemData m_slotData { get => m_owner.m_SlotDataArr[m_slotIndex]; set => m_owner.m_SlotDataArr[m_slotIndex] = value; }
		public bool isNull => m_slotData == null;

        private SlotView s_draggingFrom;
        private TextureRect s_dragIcon;
        private CanvasLayer s_dragCanvas; // 拖拽图标的父节点（全局顶层）



        public override void _Ready()
		{
			if (m_button == null || m_itemIcon == null || m_itemInfo == null)
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


    //    // -------------- 新增代码

    //    private void OnSlotGuiInput(InputEvent @event)
    //    {
    //        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
    //        {
				//if (mb.Pressed && !isNull)
				//{
				//	StartDrag();
				//}
				//else if (!mb.Pressed && s_draggingFrom != null)
				//{
    //                StopDrag();
    //            }
					
    //        }
    //        else if (@event is InputEventMouseMotion motion && s_draggingFrom != null)
    //        {
    //            s_dragIcon.GlobalPosition = motion.GlobalPosition;
    //        }
    //    }


    //    private void StartDrag()
    //    {
    //        s_draggingFrom = this;
    //        m_itemIcon.Visible = false;

    //        s_dragIcon = new TextureRect
    //        {
    //            ExpandMode = m_itemIcon.ExpandMode,
    //            Size = m_itemIcon.Size,
    //            Texture = m_itemIcon.Texture,
    //            ZIndex = 1000,
    //            TopLevel = true,
    //            MouseFilter = MouseFilterEnum.Ignore,
    //            GlobalPosition = GetGlobalMousePosition()
    //        };
    //        s_dragCanvas.AddChild(s_dragIcon);
    //    }




    }
}
