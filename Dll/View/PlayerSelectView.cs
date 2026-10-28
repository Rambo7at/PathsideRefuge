using Godot;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.UI;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.View;

/// <summary>注：玩家选择视图，负责展示人物信息与存档管理</summary>
[GlobalClass]
internal partial class PlayerSelectView : Control
{
    /// <summary>注：空数据提示标签</summary>
    [Export] private Label NullLabel;

    /// <summary>注：人物信息展示容器</summary>
    [Export] private VBoxUI PlayerInfoBox;

    /// <summary>注：存档管理列表容器</summary>
    [Export] private VBoxUI SaveManageContainer;

    /// <summary>注：创建角色按钮</summary>
    [Export] private ButtonUI CreateButton;

    /// <summary>注：管理存档按钮</summary>
    [Export] private ButtonUI ManageButton;


    /// <summary>注：当前展示的玩家UI组件</summary>
    private PlayerInfoUI PlayerInfo { get; set; }



    /// <summary>注：右侧面板的视图状态</summary>
    public enum ViewState
    {
        PlayerInfo,       // 玩家信息
        Manage,           // 存档管理
    }


    /// <summary>注：玩家信息UI组件（负责持有Label节点并更新显示）</summary>
    private class PlayerInfoUI
    {
        public PlayerInfoUI(VBoxUI vBox)
        {
            if (vBox.GetControl<Label>("Name") is not Label nameLabel) return;
            NameLabel = nameLabel;

            if (vBox.GetControl<Label>("ID") is not Label idLabel) return;
            IDLabel = idLabel;

            if (vBox.GetControl<Label>("Health") is not Label healthLabel) return;
            HealthLabel = healthLabel;

            if (vBox.GetControl<Label>("Stamina") is not Label staminaLabel) return;
            StaminaLabel = staminaLabel;

            if (vBox.GetControl<Label>("BackPack") is not Label backPackLabel) return;
            BackPackLabel = backPackLabel;

            if (NameLabel == null || IDLabel == null || HealthLabel == null || StaminaLabel == null || BackPackLabel == null)
            {
                CatLog.Warn("[PlayerSelectView.PlayerInfoUI] 存在未绑定的节点，请检查 PlayerInfoBox 的字典配置。");
            }
        }

        /// <summary>注：玩家名称标签</summary>
        private Label NameLabel { get; set; }

        /// <summary>注：玩家ID标签</summary>
        private Label IDLabel { get; set; }

        /// <summary>注：玩家血量标签</summary>
        private Label HealthLabel { get; set; }

        /// <summary>注：玩家体力标签</summary>
        private Label StaminaLabel { get; set; }

        /// <summary>注：玩家背包标签</summary>
        private Label BackPackLabel { get; set; }

        /// <summary>注：更新玩家信息显示</summary>
        public void UpdateData(CreatureData data)
        {
            if (data == null) return;

            if (NameLabel != null) NameLabel.Text = data.Name;
            if (IDLabel != null) IDLabel.Text = "ID：" + data.PlayerID.ToString();
            if (HealthLabel != null) HealthLabel.Text = "生命：" + data.Health.ToString();
            if (StaminaLabel != null) StaminaLabel.Text = "耐力：" + data.Stamina.ToString();
            if (BackPackLabel != null) BackPackLabel.Text = "背包：" + data.InventoryData?.m_itemArr?.Count.ToString() ?? "0";
        }
    }


    public override void _Ready()
    {
        if (NullLabel == null || PlayerInfoBox == null || SaveManageContainer == null || CreateButton == null || ManageButton == null)
        {
            CatLog.Err("[PlayerSelectView._Ready] 存在未赋值的节点，请检查 Inspector 面板。");
            return;
        }

        PlayerInfo ??= new(PlayerInfoBox);

        CreateButton.Pressed += () => WorldManager.Instance.ChangeScene("角色创建");
        ManageButton.Pressed += () => SwitchView(ViewState.Manage);

        SwitchView(ViewState.PlayerInfo);
    }


    /// <summary>注：切换右侧视图的显示状态，并处理前置数据逻辑</summary>
    private void SwitchView(ViewState state)
    {
        // 统一重置所有面板为隐藏，避免新增视图时漏写 Visible
        NullLabel.Visible = false;
        PlayerInfoBox.Visible = false;
        SaveManageContainer.Visible = false;

        // 分发到对应刷新方法，返回是否有有效数据
        bool hasData = state switch
        {
            ViewState.PlayerInfo => RefreshPlayerUI(),
            ViewState.Manage => RefreshPlayerListUI(),
            _ => false
        };


        // 无数据时显示空提示
        NullLabel.Visible = !hasData;

        // 管理按钮：在管理页，或没有本地玩家时禁用
        ManageButton.Disabled = state == ViewState.Manage || PlayerManager.Instance.LocalPlayerData == null;
    }


    /// <summary>注：刷新玩家信息UI展示，返回是否有有效数据</summary>
    private bool RefreshPlayerUI()
    {
        if (PlayerManager.Instance.LocalPlayerData is not CreatureData plData)
        {
            return false;
        }

        // 更新文本数据
        PlayerInfo.UpdateData(plData);

        // 更新创建按钮状态
        CreateButton.Disabled = PlayerManager.Instance.IsPlayerFull;

        PlayerInfoBox.Visible = true;
        return true;
    }


    /// <summary>注：刷新存档管理列表UI，返回是否有有效数据</summary>
    private bool RefreshPlayerListUI()
    {
        SaveManageContainer.ClearDynamicControls();

        var playerNames = PlayerManager.Instance.GetAllPlayerNames();
        if (playerNames == null || playerNames.Count == 0)
        {
            return false;
        }

        if (SaveManageContainer.GetControl<VBoxContainer>("存档列表") is not VBoxContainer listContainer)
        {
            return false;
        }

        foreach (var item in playerNames)
        {
            if (GUIManager.Instance.GetUI("SplitButtonUI") is not SplitButtonUI splitButton) continue;

            splitButton.SetMainText(item.Value);
            splitButton.SetActionText("删除");
            splitButton.DataID = item.Key;

            splitButton.SetMainAction(OnSelectPlayerSave);
            splitButton.SetActionAction(OnDeletePlayerSave);

            listContainer.AddChild(splitButton);
            SaveManageContainer.ControlArray.Add(splitButton);
        }

        SaveManageContainer.Visible = true;
        return true;
    }


    /// <summary>注：回调函数-选中某个玩家存档</summary>
    private void OnSelectPlayerSave(SplitButtonUI ui)
    {
        if (!PlayerManager.Instance.IsValidPlayer(ui.DataID)) return;

        PlayerManager.Instance.SelectedPlayerID = ui.DataID;

        // 直接调 SwitchView，内部会自动刷新数据并切回信息面板
        SwitchView(ViewState.PlayerInfo);
    }


    /// <summary>注：回调函数-删除某个玩家存档</summary>
    private void OnDeletePlayerSave(SplitButtonUI ui)
    {
        if (!PlayerManager.Instance.RemovePlayerData(ui.DataID)) return;

        ui.QueueFree();

        // 删除后请求刷新列表。如果全删光了，RefreshPlayerList 返回 false 会自动降级到空状态！
        SwitchView(ViewState.Manage);
    }
}

