using Godot;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

public partial class PlayerSaveDataView : Control
{
    [Export] Label m_nameLabel;
    [Export] Label m_bagLabel;
    [Export] Button m_toggleSaveBtn;
    [Export] VBoxContainer m_playerInfoBox;
    [Export] VBoxContainer m_saveSlotBox;

    string _butInfo;

    public override void _Ready()
    {
        if (m_nameLabel == null || m_bagLabel == null || m_toggleSaveBtn == null || m_playerInfoBox == null || m_saveSlotBox == null)
        {
            CatLog.Err("[PlayerSaveDataView._Ready] 缺少节点绑定，请检查 Inspector 面板中的导出属性。");
            CatUtils.StopAndExit(this);
            return;
        }

        m_saveSlotBox.Visible = false;
        _butInfo = m_toggleSaveBtn.Text;

        var player = PlayerManager.Instance.LocalPlayerData;
        if (player != null)
        {
            ApplyPlayerInfo(player);
        }

        RefreshSaveSlots();
    }

    public override void _Process(double delta)
    {
        var data = PlayerManager.Instance.LocalPlayerData;
        if (data == null) return;

        ApplyPlayerInfo(data);
    }

    private void Creator() => WorldManager.Instance.ChangeScene("角色创建");

    private void OpenSaveSelection()
    {
        m_saveSlotBox.Visible = !m_saveSlotBox.Visible;
        m_playerInfoBox.Visible = !m_playerInfoBox.Visible;
        m_toggleSaveBtn.Text = m_toggleSaveBtn.Text == _butInfo ? "返回" : _butInfo;
    }

    private void ApplyPlayerInfo(CreatureData data)
    {
        m_nameLabel.Text = "玩家名：" + data.Name;
        m_bagLabel.Text = "背包库存：" + data.GetInventoryItemCount();
    }

    private void RefreshSaveSlots()
    {
        // 清空旧槽位
        var children = m_saveSlotBox.GetChildren();
        foreach (var child in children)
        {
            child.QueueFree();
        }

        var ids = PlayerManager.Instance.GetAllPlayerIDs();
        if (ids == null || ids.Count == 0) return;

        foreach (int id in ids)
        {
            var slot = GUIManager.Instance.GetUI("Button_A1") as Button;
            if (slot == null) continue;

            slot.Text = "ID:" + id;
            m_saveSlotBox.AddChild(slot);

            slot.Pressed += () => OnButtonPressed(id);

        }
    }

    private void OnButtonPressed(int ID)
    {
        PlayerManager.Instance.SelectedPlayerID = ID;
        OpenSaveSelection();
    }



}
