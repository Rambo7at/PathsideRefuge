using Godot;
using 维修公司.Dll.data;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
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
        public IInventoryHolder m_holder { get; set; }
        private ItemData m_slotData
        {
            get => m_holder.InventoryData.m_itemArr[m_slotIndex];
            set => m_holder.InventoryData.m_itemArr[m_slotIndex] = value;
        }
        public bool isNull => m_slotData == null;

        public override void _Ready()
        {
            if (m_button == null || m_itemIcon == null || m_itemInfo == null)
            {
                CatLog.Err($"[SlotView._Ready]：检测需求字段 有空 已销毁");
                CatUtils.StopAndExit(this);
                return;
            }

            m_button.GuiInput += OnSlotGuiInput;   
            Refresh();
        }

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

        private void OnSlotGuiInput(InputEvent @event)
        {
            var gui = PlayerManager.Instance.m_CanvasLayer;

            // 移动时更新拖拽图标位置（只要全局有拖拽存在）
            if (@event is InputEventMouseMotion motion && gui.CurrentDragIcon != null)
            {
                gui.CurrentDragIcon.GlobalPosition = motion.GlobalPosition;
                return;
            }

            if (@event is not InputEventMouseButton mb) return;

            if (mb.ButtonIndex == MouseButton.Left)
            {
                // 按下左键：只有当前格子非空且全局没有拖拽时才启动
                if (mb.Pressed && !isNull && gui.CurrentDragSource == null)
                {
                    StartDrag(gui);
                }
                // 释放左键：如果全局存在拖拽源，执行停止
                else if (!mb.Pressed && gui.CurrentDragSource != null)
                {
                    StopDrag(gui);
                }
            }
        }

        private void StartDrag(PlayerGUI gui)
        {
            gui.CurrentDragSource = this;
            m_itemIcon.Visible = false;

            gui.CurrentDragIcon = new TextureRect
            {
                ExpandMode = m_itemIcon.ExpandMode,
                Size = m_itemIcon.Size,
                Texture = m_itemIcon.Texture,
                ZIndex = 1000,
                TopLevel = true,
                MouseFilter = MouseFilterEnum.Ignore,
                GlobalPosition = GetGlobalMousePosition()
            };
            gui.AddChild(gui.CurrentDragIcon);
        }

        private void StopDrag(PlayerGUI gui)
        {
            var source = gui.CurrentDragSource;
            if (source == null) return;

            // 清理图标
            gui.CurrentDragIcon?.QueueFree();
            gui.CurrentDragIcon = null;

            // 恢复源图标
            source.m_itemIcon.Visible = true;

            // 查找目标格子（全局查找，支持跨容器）
            SlotView targetSlot = GetHoveredSlot();

            if (targetSlot == null)
            {
                // 丢弃到世界
                source.m_slotData?.TryDropItem(source.m_holder.DropPos);
                source.m_slotData = null;
                Refresh();
            }
            else if (targetSlot != source)
            {
                // 交换（同容器或跨容器都直接交换数据引用）
                var itemA = targetSlot.m_slotData;
                var itemB = source.m_slotData;
                targetSlot.m_slotData = itemB;
                source.m_slotData = itemA;

                targetSlot.Refresh();
                source.Refresh();
            }

            gui.CurrentDragSource = null;
        }

        private SlotView GetHoveredSlot()
        {
            Control hovered = ((SceneTree)Engine.GetMainLoop()).Root.GuiGetHoveredControl();
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
