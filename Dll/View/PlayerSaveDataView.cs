using Godot;
using Godot.Collections;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

public partial class PlayerSaveDataView : Control
{
    [Export] Label NameLabel;
    [Export] Label BagLabel;
    [Export] Button ToggleSaveBtn;
    [Export] VBoxContainer PlayerInfoBox;
    [Export] VBoxContainer SaveSlotBox;

    private Array<PlayerSaveSlotComp> SaveBoxArray = [];
    string Pickinfo;

    public override void _Ready()
    {
        Pickinfo = ToggleSaveBtn.Text;
        SaveSlotBox.Visible = false;
        PlayerInfoBox.Visible = true;
        RefreshSaveBox();
    }

    public override void _Process(double delta)
    {
        PlayerData playerData = SaveManager.Instance.GetSelectedPlayerData();
        if (playerData == null) return;

        ApplyPlayerInfo(playerData);
        PlayerManager.Instance.m_LocalPlayerData = playerData.DeepCopy();
    }

    private void OpenSaveSelection()
    {
        SaveSlotBox.Visible = !SaveSlotBox.Visible;
        PlayerInfoBox.Visible = !PlayerInfoBox.Visible;

        if (SaveSlotBox.Visible == true) ToggleSaveBtn.Text = "返回";
        else ToggleSaveBtn.Text = Pickinfo;
    }

    private void Creator()
    {
        CatUtils.ChangeScene(this, "角色创建");
        return;
    }

    private void ApplyPlayerInfo(PlayerData playerData)
    {
        if (playerData == null) return;
        NameLabel.Text = "玩家名：" + playerData.m_Name;
        BagLabel.Text = "背包库存：" + playerData.GetInventoryItemCount();
    }

    private void RefreshSaveBox()
    {
        if (SaveBoxArray.Count != 0)
        {
            foreach (var item in SaveBoxArray)
            {
                item.QueueFree();
            }
        }
        SaveBoxArray.Clear();

        var IDs = SaveManager.Instance.GetAllPlayerIDs();
        if (IDs.Count <= 1) return;

        for (int i = 0; i < IDs.Count; i++)
        {
            var ui = UIManager.Instance.GetUI("存档信息") as PlayerSaveSlotComp;
            if (ui == null) return;

            ui.m_PlayerID = IDs[i];
            SaveSlotBox.AddChild(ui);
        }
    }
}
