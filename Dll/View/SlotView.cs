using Godot;
using Godot.Collections;
using System;
using 维修公司.Dll.data;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using static 维修公司.Dll.data.ItemData;

namespace 途畔归所.Dll.View
{
    /// <summary>注：UI格子视图，支持物品显示、拖拽交换与装备槽位校验。</summary>
    public partial class SlotView : Control
    {
        [ExportGroup("基础")]
        [Export] public bool IsEquipSlot = false;          // 是否为装备槽
        [Export] public Button Button;                     // 交互按钮
        [Export] public TextureRect ItemIcon;              // 物品图标
        [Export] public Label ItemInfo;                    // 物品信息文本


        [ExportGroup("格子类型")]
        [Export] public E_EquipAVL SlotType = E_EquipAVL.None; // 槽位类型（主手/副手）

        private ItemData m_slotData { get => OnGetItem?.Invoke(); set => OnSetItem?.Invoke(value); }
        private Vector3 m_DropPos => OnDropPos?.Invoke() == null ? new Vector3() : OnDropPos.Invoke();

        public bool IsNull => m_slotData == null;

        public Func<Vector3> OnDropPos;
        public Func<ItemData> OnGetItem;
        public Action<ItemData> OnSetItem;

        /// <summary>注：初始化格子视图，绑定输入事件。</summary>
        public override void _Ready()
        {
            if (Button == null || ItemIcon == null || ItemInfo == null)
            {
                CatLog.Err($"[SlotView._Ready]：检测需求字段 有空 已销毁");
                CatUtils.StopAndExit(this);
                return;
            }

            if (OnDropPos == null || OnGetItem == null || OnSetItem == null)
            {
                CatLog.Err($"[SlotView._Ready]：检测委托未有进行绑定，请检查父对象：{this.GetParent().Name}");
            }

            Button.GuiInput += OnSlotGuiInput;
            Refresh();
        }

        /// <summary>注：刷新格子显示（物品图标、名称、数量）。</summary>
        public void Refresh()
        {
            if (m_slotData == null)
            {
                ItemInfo.Text = string.Empty;
                ItemIcon.Texture = null;
                ItemIcon.Visible = true;
            }
            else
            {
                ItemInfo.Text = $"{m_slotData.Name} x{m_slotData.Stack}";
                ItemIcon.Texture = m_slotData.Icon;
            }
        }

        /// <summary>注：处理格子上的鼠标输入事件（拖拽开始/结束）。</summary>
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
                if (mb.Pressed && !IsNull && gui.CurrentDragSource == null)
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

        /// <summary>注：开始拖拽（创建拖拽图标）。</summary>
        private void StartDrag(PlayerGUI gui)
        {
            gui.CurrentDragSource = this;
            ItemIcon.Visible = false;

            gui.CurrentDragIcon = new TextureRect
            {
                ExpandMode = ItemIcon.ExpandMode,
                Size = ItemIcon.Size,
                Texture = ItemIcon.Texture,
                ZIndex = 1000,
                TopLevel = true,
                MouseFilter = MouseFilterEnum.Ignore,
                GlobalPosition = GetGlobalMousePosition()
            };
            gui.AddChild(gui.CurrentDragIcon);
        }

        /// <summary>注：结束拖拽（清理图标、查找目标并执行交换或丢弃）。</summary>
        private void StopDrag(PlayerGUI gui)
        {
            var source = gui.CurrentDragSource;
            if (source == null) return;

            // 清理图标
            gui.CurrentDragIcon?.QueueFree();
            gui.CurrentDragIcon = null;

            // 恢复源图标
            source.ItemIcon.Visible = true;

            // 查找目标格子
            SlotView targetSlot = GetHoveredSlot();

            if (targetSlot == null)
            {
                // 丢弃到世界
                source.m_slotData?.TryDropItem(source.m_DropPos);
                source.m_slotData = null;
                Refresh();
                gui.CurrentDragSource = null;
                return;
            }

            if (targetSlot == source)
            {
                gui.CurrentDragSource = null;
                return;
            }

            // 执行交换（内部处理合法性校验）
            SwapItems(source, targetSlot);

            gui.CurrentDragSource = null;
        }

        /// <summary>注：交换两个槽位的物品数据。</summary>
        private void SwapItems(SlotView fromSlot, SlotView toSlot)
        {
            var fromData = fromSlot.m_slotData;
            var toData = toSlot.m_slotData;

            // 1. 校验：目标槽是装备槽时，检查拖过来的物品能否放入
            if (toSlot.IsEquipSlot && fromData != null)
            {
                if (toSlot.SlotType == E_EquipAVL.MainHand && !fromData.CanEquipMainHand)
                {
                    CatLog.Debug($"[SwapItems] 物品 {fromData.Name} 不能放入主手槽位");
                    return;
                }

                if (toSlot.SlotType == E_EquipAVL.OffHand && !fromData.CanEquipOffHand)
                {
                    CatLog.Debug($"[SwapItems] 物品 {fromData.Name} 不能放入副手槽位");
                    return;
                }
            }

            // 2. 校验：源槽是装备槽时，检查目标物品能否放入源槽（交换场景）
            if (fromSlot.IsEquipSlot && toData != null)
            {
                if (fromSlot.SlotType == E_EquipAVL.MainHand && !toData.CanEquipMainHand)
                {
                    CatLog.Debug($"[SwapItems] 物品 {toData.Name} 不能放入源主手槽位");
                    return;
                }

                if (fromSlot.SlotType == E_EquipAVL.OffHand && !toData.CanEquipOffHand)
                {
                    CatLog.Debug($"[SwapItems] 物品 {toData.Name} 不能放入源副手槽位");
                    return;
                }
            }

            // 3. 执行交换
            toSlot.m_slotData = fromData;
            fromSlot.m_slotData = toData;

            toSlot.Refresh();
            fromSlot.Refresh();
        }

        /// <summary>注：获取当前鼠标悬停的格子视图。</summary>
        private SlotView GetHoveredSlot()
        {
            Control hovered = ((SceneTree)Engine.GetMainLoop()).Root.GuiGetHoveredControl();
            while (hovered != null)
            {
                if (hovered is SlotView slot) return slot;

                hovered = hovered.GetParent() as Control;
            }
            return null;
        }
    }

}
