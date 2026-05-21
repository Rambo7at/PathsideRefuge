using Godot;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.View
{
    public partial class WorldSaveDataView : Control
    {

        [Export] Label m_nameLabel;
        [Export] Button m_toggleSaveButton;
        [Export] VBoxContainer m_worldInfoBox;         // 原 PlayerInfoBox，只显示世界名
        [Export] VBoxContainer m_saveSlotBox;

        [Export] Control m_inputField;
        [Export] LineEdit m_inputTextBox;

        string _butInfo;

        public override void _Ready()
        {
            if (m_nameLabel == null || m_toggleSaveButton == null || m_worldInfoBox == null || m_saveSlotBox == null)
            {
                CatLog.Err("[WorldSaveDataView] 存在未赋值的导出控件，已跳过初始化。");
                CatUtils.StopAndExit(this);
                return;
            }

            m_saveSlotBox.Visible = false;

            _butInfo = m_toggleSaveButton.Text;

            var wolde = SaveManager.Instance.GetSelectedWorldData();

            if (wolde != null) m_nameLabel.Text = m_nameLabel.Text = "世界：" + wolde.m_name;

            RefreshSaveSlots();
        }



        public override void _Process(double delta)
        {
            var data = SaveManager.Instance.GetSelectedWorldData();
            if (data == null) return;

            m_nameLabel.Text = data.m_name;
        }

        private void Creator() => m_inputField.Visible = true;


        private void OnConfirmWorldName()
        {
            
            string name = m_inputTextBox.Text;

            if (string.IsNullOrEmpty(name)) return;

            SaveManager.Instance.CreateWorld(name);

            RefreshSaveSlots();

            m_inputField.Visible = false;
        }


        private void OpenSaveSelection()
        {
            m_saveSlotBox.Visible = !m_saveSlotBox.Visible;
            m_worldInfoBox.Visible = !m_worldInfoBox.Visible;
            m_toggleSaveButton.Text = m_toggleSaveButton.Text == _butInfo ? "返回" : _butInfo;
        }




        private void RefreshSaveSlots()
        {
            var savedata = m_saveSlotBox.GetChildren();

            if (savedata == null) return;

            foreach (var save in savedata)
            {
                save.QueueFree();
            }

            var IDarr = SaveManager.Instance.GetAllWorldIDs();
            if (IDarr == null || IDarr.Count == 0) return;

            foreach (var id in IDarr)
            {
                var ui = UIManager.Instance.GetUI("存档信息") as PlayerSaveSlotComp;
                if (ui == null) continue;
                ui.m_PlayerID = id;
                m_saveSlotBox.AddChild(ui);
            }

        }
    }
}
