using Godot;
using Godot.Collections;
using System;
using 维修公司.Dll.data;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using 途畔归所.Dll.View;

namespace 途畔归所.Dll.Creature
{
    [GlobalClass]
    public partial class PlayerGUI : CanvasLayer, IInventoryHolder, IEquipmentHolder
    {
        [Export] private Node3D m_dropPos;

        // 组件
        private Player m_player;
        private StateMachine m_StateMachine;

        // UI 组件
        private InventoryView m_inventoryView;
        private ConsoleView m_consoleView;
        private EscView m_escView;
        private HudView m_hudView;
        private EquipmentView m_EquipmentView;
        private DialogView m_DialogView;


        // 属性
        public SlotView CurrentDragSource { get; set; }
        public TextureRect CurrentDragIcon { get; set; }
        public Vector3 m_DropPos => m_dropPos.GlobalPosition;

        public override void _Ready()
        {

            if (GetParent() is not Player pl)
            {
                CatLog.Err($"[PlayerGUI._Ready]：检测挂载对象并非 player 或是空 ，已销毁");
                CatUtils.StopAndExit(this);
                return;
            }

            m_player = pl;
            m_StateMachine = pl.m_StateMachine;

            if (pl.m_IsOwner == false)
            {
                CatUtils.StopAndExit(this);
                CatLog.Net($"[PlayerGUI._Ready]：当前并非本地玩家，已销毁");
                return;
            }

            InitPlayerHUD();
            InitConsole();
            InitEsc();
            InitPlayerEquip();
            InitDialog();
            InitInventory();
        }

        public override void _Process(double delta)
        {
            ProcessUIInputs();
            UpdateMouseMode();
        }

        private void InitInventory()
        {
            // 直接创建 InventoryView 并通过 UIManager 获取预制体
            if (UIManager.Instance.GetUI(InventoryData.m_UIname) is not InventoryView view)
            {
                CatLog.Err("[PlayerGUI.InitInventory] 背包视图加载失败");
                return;
            }

            m_inventoryView = view;
            m_inventoryView.Visible = false;
            AddChild(m_inventoryView);
        }

        private void InitConsole()
        {
            if (m_consoleView != null) return;

            if (UIManager.Instance.GetUI("ConsoleUI") is not ConsoleView view) return;

            m_consoleView = view;
            m_consoleView.GetPlayer(m_player);
            view.Visible = false;
            AddChild(view);
        }

        private void InitEsc()
        {
            if (m_escView != null) return;

            if (UIManager.Instance.GetUI("esc_ui") is not EscView view) return;

            m_escView = view;
            view.Visible = false;
            AddChild(view);
        }

        private void InitPlayerHUD()
        {
            m_hudView ??= UIManager.Instance.GetUI("hud") is HudView hud ? hud : null;
            if (m_hudView == null) return;

            m_hudView.m_maxHP = m_player.m_Health;
            m_hudView.Visible = true;
            AddChild(m_hudView);
        }

        private void InitPlayerEquip()
        {
            m_EquipmentView ??= UIManager.Instance.GetUI("EquipUI") is EquipmentView view ? view : null;
            if (m_EquipmentView == null)
            {
                CatLog.Warn("这个信息是空的！");
                return;
            }

            m_EquipmentView.Visible = false;
            AddChild(m_EquipmentView);
        }

        private void InitDialog()
        {
            m_DialogView ??= UIManager.Instance.GetUI("DialogView") is DialogView view ? view : null;

            if (m_DialogView == null)
            {
                CatLog.Warn("获取 m_DialogView 失败");
                return;
            }

            m_DialogView.Visible = false;
            AddChild(m_DialogView);
        }


        /// <summary>注：处理与 UI 相关的按键输入。</summary>
        private void ProcessUIInputs()
        {
            if (Input.IsActionJustPressed("cat_Console")) m_consoleView.ToggleUI();
            if (Input.IsActionJustPressed("cat_Tab"))
            {
                m_inventoryView.ToggleUI();
                m_EquipmentView.ToggleUI();
            }
            if (Input.IsActionJustPressed("cat_Esc")) m_escView.ToggleUI();
        }

        /// <summary>注：根据当前打开的 UI 面板自动切换鼠标模式与 UI 状态标志。</summary>
        private void UpdateMouseMode()
        {
            // 用视图的 Visible 替代原来的 Ui_Visible 委托
            if (m_consoleView.Visible || m_escView.Visible || m_inventoryView.Visible)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                m_StateMachine.SwitchPlayerState(StateMachine.PlayerState.Menu);
            }
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
                m_StateMachine.SwitchPlayerState(StateMachine.PlayerState.Idle);
            }
        }


        public DialogView GetDialogView() => m_DialogView == null ? null : m_DialogView;



        #region 接口实现
        public InventoryData InventoryData { get => m_player.m_InventoryData ??= new InventoryData(); set => m_player.m_InventoryData = value; }
        Vector3 IInventoryHolder.DropPos => m_DropPos;

        public Vector3 DropPos => m_DropPos;

        public Equipment Equipment { get => m_player.m_Equipment; set => m_player.m_Equipment = value; }

        #endregion
    }
}
