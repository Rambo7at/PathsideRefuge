using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 维修公司.Dll.data;
using 途畔归所.Dll.Comp;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;

namespace 途畔归所.Dll.Creature
{
    public class PlayerUIHandler : IInventoryHolder
    {
        private Player m_player;
        public InventoryComp m_InventoryComp;
        private ConsoleComp m_ConsoleComp;
        private EscComp m_EscComp;
        private CanvasLayer m_CanvasLayer;

        public PlayerUIHandler(Player pl)
        {
            m_player ??= pl;
            m_CanvasLayer = pl.m_CanvasLayer;

            InitInventory();
            InitConsole();
            InitEsc();
        }

        public void Updata()
        {
            ProcessUIInputs();
            UpdateMouseMode();
        }


        private void InitInventory()
        {
            if (m_InventoryComp != null) return;

            var UI = UIManager.Instance.GetUI("InventoryUI");
            if (UI == null) return;
            if (UI is not InventoryComp script) return;

            script.Holder = this;
            m_InventoryComp = script;
            UI.Visible = false;
            m_CanvasLayer.AddChild(UI);
        }
        private void InitConsole()
        {
            if (m_ConsoleComp != null) return;

            var UI = UIManager.Instance.GetUI("ConsoleUI");
            if (UI == null) return;
            if (UI is not ConsoleComp script) return;

            m_ConsoleComp = script;
            m_ConsoleComp.GetPlayer(m_player);
            UI.Visible = false;
            m_CanvasLayer.AddChild(UI);
        }
        private void InitEsc()
        {
            if (m_EscComp != null) return;

            var UI = UIManager.Instance.GetUI("esc_ui");
            if (UI == null) return;
            if (UI is not EscComp script) return;

            m_EscComp = script;
            UI.Visible = false;
            m_CanvasLayer.AddChild(UI);
        }



        /// <summary>注：处理与 UI 相关的按键输入。</summary>
        private void ProcessUIInputs()
        {
            if (Input.IsActionJustPressed("cat_Console")) m_ConsoleComp.ToggleUI();
            if (Input.IsActionJustPressed("cat_Tab"))
            {
                m_InventoryComp.RefSlot();
                m_InventoryComp.ToggleUI();
            }
            if (Input.IsActionJustPressed("cat_Esc")) m_EscComp.ToggleUI();
        }


        /// <summary>注：根据当前打开的 UI 面板自动切换鼠标模式与 UI 状态标志。</summary>
        private void UpdateMouseMode()
        {
            if (m_ConsoleComp.Visible || m_InventoryComp.Visible || m_EscComp.Visible)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;

                m_player.m_OnUI = true;
            }
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
                m_player.m_OnUI = false;
            }
        }


        public CanvasLayer GetCanvasLayer() => m_CanvasLayer;

        public Vector3 GetDropPosition() => m_player.m_eye.GlobalPosition + m_player.m_eye.GlobalBasis.Z * -1.0f;

        public Godot.Collections.Dictionary<int, ItemData> LoadInventory() => m_player.m_PlayerData.m_InventoryData ?? [];

        public void SaveInventory(Array<SlotComp> slotComps) => m_player.m_PlayerData.UpdateInventoryData(slotComps);
    }
}
