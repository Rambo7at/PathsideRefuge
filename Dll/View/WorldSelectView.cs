using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.UI;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.View;

/// <summary>注：世界选择视图，负责本地世界与多人房间的管理及进入</summary>
public partial class WorldSelectView : Control
{
    /// <summary>注：本地游戏按钮</summary>
    [Export] public ButtonUI LocalGameButton { get; set; }

    /// <summary>注：多人游戏按钮</summary>
    [Export] public ButtonUI MultiplayerButton { get; set; }

    /// <summary>注：世界信息按钮（导航）</summary>
    [Export] public ButtonUI WorldInfoButton { get; set; }

    /// <summary>注：创建世界按钮（导航）</summary>
    [Export] public ButtonUI CreateWorldButton { get; set; }

    /// <summary>注：管理世界按钮（导航）</summary>
    [Export] public ButtonUI ManageWorldButton { get; set; }

    /// <summary>注：搜索房间按钮（导航）</summary>
    [Export] public ButtonUI SearchRoomButton { get; set; }


    /// <summary>注：世界信息展示容器</summary>
    [Export] public VBoxUI WorldInfoBox { get; set; }

    /// <summary>注：存档列表容器</summary>
    [Export] public VBoxUI SaveManageContainer { get; set; }

    /// <summary>注：房间列表容器</summary>
    [Export] public VBoxUI RoomInfoContainer { get; set; }

    /// <summary>注：创建世界容器</summary>
    [Export] public VBoxUI CreateWorldBox { get; set; }

    /// <summary>注：空数据提示标签</summary>
    [Export] public Label NullLabel;


    /// <summary>注：当前展示的世界UI组件</summary>
    private WorldInfoUI WorldInfo { get; set; }

    /// <summary>注：房间搜索中的加载提示标签</summary>
    private Label LoadingLabel { get; set; }

    /// <summary>注：是否正在播放加载动画</summary>
    private bool _isAnimatingLoading = false;

    /// <summary>注：右侧面板的视图状态</summary>
    public enum ViewState
    {
        WorldInfo,       // 世界信息
        WorldInfoNone,   // 世界信息（无数据状态）
        SaveManage,      // 存档管理
        RoomInfo,        // 房间信息
        CreateWorld,     // 创建世界
        None             // 全部隐藏
    }


    /// <summary>注：世界信息UI组件（负责持有Label节点并更新显示）</summary>
    private class WorldInfoUI
    {
        public WorldInfoUI(VBoxUI vBox)
        {
            if (vBox.GetControl<Label>("WorldName") is not Label nameLabel) return;
            WorldNameLabel = nameLabel;

            if (vBox.GetControl<Label>("ID") is not Label idLabel) return;
            WorldIDLabel = idLabel;

            if (WorldNameLabel == null || WorldIDLabel == null)
            {
                CatLog.Warn("[WorldSelectView.WorldInfoUI] 存在未绑定的节点，请检查 WorldInfoBox 的字典配置。");
            }
        }

        /// <summary>注：世界名称标签</summary>
        private Label WorldNameLabel { get; set; }

        /// <summary>注：世界ID标签</summary>
        private Label WorldIDLabel { get; set; }

        /// <summary>注：更新世界信息显示</summary>
        public void UpdateData(Data.WorldData wdt)
        {
            if (WorldNameLabel != null) WorldNameLabel.Text = wdt.Name;
            if (WorldIDLabel != null) WorldIDLabel.Text = wdt.WorldID.ToString();
        }
    }

    public override void _Ready()
    {
        // 节点检查
        if (LocalGameButton == null || MultiplayerButton == null || WorldInfoButton == null ||
            CreateWorldButton == null || ManageWorldButton == null || SearchRoomButton == null ||
            WorldInfoBox == null || SaveManageContainer == null || RoomInfoContainer == null ||
            CreateWorldBox == null || NullLabel == null)
        {
            CatLog.Err("[WorldSelectView._Ready] 存在未赋值的节点，请检查 Inspector 面板。");
            return;
        }

        // 获取加载标签
        LoadingLabel = RoomInfoContainer.GetControl<Label>("Load");
        if (LoadingLabel == null)
        {
            CatLog.Warn("[WorldSelectView] 未找到名为 'Load' 的加载标签，加载动画将无法显示。");
        }

        WorldInfo = new(WorldInfoBox);

        SwitchView(ViewState.WorldInfo);

        BindButtonEvents();
        InitCreateWorldUI();
    }


    /// <summary>注：初始化创建世界面板的UI与事件绑定</summary>
    private void InitCreateWorldUI()
    {
        if (CreateWorldBox.GetControl<LineEdit>("输入框") is not LineEdit le)
        {
            CatLog.Err("[WorldSelectView.InitCreateWorldUI] 存在未赋值的输入框节点，请检查 Inspector 面板。");
            return;
        }
        if (CreateWorldBox.GetControl<Button>("确认按钮") is not Button bt)
        {
            CatLog.Err("[WorldSelectView.InitCreateWorldUI] 存在未赋值的确认按钮节点，请检查 Inspector 面板。");
            return;
        }

        bt.Disabled = true;

        // 绑定确认按钮
        bt.Pressed += () =>
        {
            WorldManager.Instance.CreateWorld(le.Text);

            // 创建成功后，重置表单状态
            le.Text = "";
            bt.Disabled = true;

            SwitchView(ViewState.WorldInfo); // 切回世界信息面板
        };

        // 绑定输入框文本变化事件
        le.TextChanged += (string text) =>
        {
            // 【注意】这里的 IsValidWorldName 如果是返回 true 代表可用，则需要去掉 !
            // 如果是返回 true 代表重名/不可用，则保留 ! 
            bool isValid = !string.IsNullOrWhiteSpace(text) && text.Length <= 10 && !WorldManager.Instance.IsValidWorldName(text);

            bt.Disabled = !isValid;
        };
    }


    /// <summary>注：切换右侧视图的显示状态，并处理前置数据逻辑</summary>
    private void SwitchView(ViewState state)
    {
        // 切换视图时，强制停止加载动画
        StopLoadingAnimation();

        // 统一隐藏所有面板，避免新增视图时漏写 Visible
        WorldInfoBox.Visible = false;
        SaveManageContainer.Visible = false;
        RoomInfoContainer.Visible = false;
        CreateWorldBox.Visible = false;
        NullLabel.Visible = false;

        // 分发到对应刷新方法，返回是否有有效内容
        bool hasContent = state switch
        {
            ViewState.WorldInfo => RefreshWorldInfo(),
            ViewState.SaveManage => RefreshSaveList(),
            ViewState.RoomInfo => RefreshRoomInfo(),
            ViewState.CreateWorld => PrepareCreateWorld(),
            ViewState.WorldInfoNone => ShowEmptyState(),
            ViewState.None => false,
            _ => false
        };

        // 无内容时显示空提示
        NullLabel.Visible = !hasContent;

        // 导航按钮状态：当前视图对应的按钮禁用（避免重复点击）
        WorldInfoButton.Disabled = state == ViewState.WorldInfo || state == ViewState.WorldInfoNone;
        CreateWorldButton.Disabled = state == ViewState.CreateWorld;
        ManageWorldButton.Disabled = !WorldManager.Instance.HasWorlds || state == ViewState.SaveManage;
        SearchRoomButton.Disabled = state == ViewState.RoomInfo;
    }


    /// <summary>注：刷新世界信息展示，返回是否有有效数据</summary>
    private bool RefreshWorldInfo()
    {
        if (WorldManager.Instance.CurrentWorld is not Data.WorldData wdt)
        {
            SetGameButtonsEnabled(false);
            return false;
        }

        SetGameButtonsEnabled(true);
        WorldInfo.UpdateData(wdt);

        WorldInfoBox.Visible = true;
        return true;
    }


    /// <summary>注：刷新存档管理列表，返回是否有有效数据</summary>
    private bool RefreshSaveList()
    {
        var worldNames = WorldManager.Instance.GetAllWorldNames();
        if (worldNames == null || worldNames.Count == 0)
        {
            SetGameButtonsEnabled(false);
            return false;
        }

        SaveManageContainer.ClearDynamicControls();

        if (SaveManageContainer.GetControl<VBoxContainer>("存档列表") is not VBoxContainer saveList)
        {
            SetGameButtonsEnabled(false);
            return false;
        }

        foreach (var worldName in worldNames)
        {
            if (GUIManager.Instance.GetUI("SplitButtonUI") is not SplitButtonUI splitButton) continue;

            splitButton.SetMainText(worldName.Value);
            splitButton.SetActionText("删除");
            splitButton.DataID = worldName.Key;

            splitButton.SetMainAction(OnSelectWorldSave);
            splitButton.SetActionAction(OnDeleteWorldSave);

            saveList.AddChild(splitButton);
            SaveManageContainer.ControlArray.Add(splitButton);
        }

        SaveManageContainer.Visible = true;
        return true;
    }


    /// <summary>注：刷新房间列表，触发异步网络搜索并返回是否准备就绪</summary>
    private bool RefreshRoomInfo()
    {
        RoomInfoContainer.ClearDynamicControls();

        // 1. 先显示容器，否则加载动画出不来
        RoomInfoContainer.Visible = true;

        // 2. 开启加载动画
        StartLoadingAnimation();

        // 3. 触发局域网广播搜索
        NetCore.Instance.FindLANRoom();

        // 4. 异步等待网络回包并更新列表
        WaitAndUpdateRoomList();

        // 5. 强制返回 true，告诉 SwitchView "我正在加载中，千万别弹 NullLabel"
        return true;
    }


    /// <summary>注：异步等待网络回包，并更新房间列表</summary>
    private async void WaitAndUpdateRoomList()
    {
        // 等待 2 秒（给局域网 UDP 广播留足时间）
        await ToSignal(GetTree().CreateTimer(2.0f), "timeout");

        // 安全检查：如果节点被销毁，或者玩家已经切走了，直接退出
        if (!IsInstanceValid(this) || !_isAnimatingLoading) return;

        var rooms = NetCore.Instance.GetLANRooms();

        // 搜索结束，关掉动画
        StopLoadingAnimation();

        // 如果没搜到房间，显示空提示
        if (rooms == null || rooms.Count == 0)
        {
            NullLabel.Visible = true;
            return;
        }

        // 搜到了房间，开始生成列表
        if (RoomInfoContainer.GetControl<VBoxContainer>("房间列表") is not VBoxContainer roomList) return;

        foreach (var room in rooms)
        {
            if (GUIManager.Instance.GetUI("SplitButtonUI") is not SplitButtonUI splitButton) continue;

            splitButton.SetMainText($"{room.RoomName} ({room.IP})");
            splitButton.SetActionText("加入");

            // 【关键】直接把 IP 塞进 UserData，解决之前用 Hash 导致的隐患
            splitButton.UserData = room.IP;

            // 房间不需要删除按钮，直接隐藏右侧副按钮
            splitButton.ActionButton.Visible = false;

            splitButton.SetMainAction(OnJoinRoom);

            roomList.AddChild(splitButton);
            RoomInfoContainer.ControlArray.Add(splitButton);
        }
    }


    /// <summary>注：启动加载动画</summary>
    private async void StartLoadingAnimation()
    {
        if (LoadingLabel == null || _isAnimatingLoading) return;

        _isAnimatingLoading = true;
        LoadingLabel.Visible = true;

        // 重置透明度，防止重复启动时动画重叠
        LoadingLabel.Modulate = new Color(1, 1, 1, 1);

        // 1. Tween 透明度呼吸效果
        Tween tween = CreateTween().SetLoops();
        tween.TweenProperty(LoadingLabel, "modulate:a", 0.3f, 0.6f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(LoadingLabel, "modulate:a", 1.0f, 0.6f).SetTrans(Tween.TransitionType.Sine);

        // 2. 文本循环动画
        string[] dots = { ".", "..", "..." };
        int index = 0;

        while (_isAnimatingLoading)
        {
            LoadingLabel.Text = $"正在搜索房间{dots[index]}";
            index = (index + 1) % 3;

            // 利用 Godot 自带的场景树 Timer 等待 0.4 秒
            await ToSignal(GetTree().CreateTimer(0.4f), "timeout");

            // 安全防线：如果在等待期间，玩家退出了界面导致节点被销毁，直接 return
            if (!IsInstanceValid(this) || !_isAnimatingLoading) return;
        }
    }


    /// <summary>注：停止加载动画</summary>
    private void StopLoadingAnimation()
    {
        _isAnimatingLoading = false;

        if (LoadingLabel != null && IsInstanceValid(LoadingLabel))
        {
            LoadingLabel.Visible = false;
            LoadingLabel.Modulate = new Color(1, 1, 1, 1); // 恢复初始透明度
        }
    }


    /// <summary>注：回调函数-加入某个局域网房间</summary>
    private void OnJoinRoom(SplitButtonUI ui)
    {
        if (ui.UserData is not string ip)
        {
            CatLog.Err("[WorldSelectView.OnJoinRoom] 未获取到有效的房间 IP 信息。");
            return;
        }

        CatLog.Ok($"[WorldSelectView] 正在加入房间: {ip}");

        // 在这里调用你 NetCore 里的加入房间逻辑，比如：
        // var result = NetCore.Instance.JoinLANRoom(ip);
        // if (result == Error.Ok) { GameCore.Instance.EnterGame(); }
    }


    /// <summary>注：准备创建世界面板，重置表单状态</summary>
    private bool PrepareCreateWorld()
    {
        if (CreateWorldBox.GetControl<LineEdit>("输入框") is LineEdit le)
        {
            le.Text = "";
            le.GrabFocus(); // 自动获取焦点，玩家可以直接打字
        }
        if (CreateWorldBox.GetControl<Button>("确认按钮") is Button bt)
        {
            bt.Disabled = true;
        }

        CreateWorldBox.Visible = true;
        return true;
    }


    /// <summary>注：显示空状态，禁用游戏按钮</summary>
    private bool ShowEmptyState()
    {
        SetGameButtonsEnabled(false);
        return false;
    }


    /// <summary>注：设置游戏按钮的启用/禁用状态</summary>
    private void SetGameButtonsEnabled(bool isEnable)
    {
        LocalGameButton.Disabled = !isEnable;
        MultiplayerButton.Disabled = !isEnable;
    }


    /// <summary>注：绑定所有按钮的点击事件</summary>
    private void BindButtonEvents()
    {
        LocalGameButton.Pressed += () => GameCore.Instance.EnterGame();

        MultiplayerButton.Pressed += () =>
        {
            NetCore.Instance.StartLANHost();
            GameCore.Instance.EnterGame();
        };

        WorldInfoButton.Pressed += () => SwitchView(ViewState.WorldInfo);
        ManageWorldButton.Pressed += () => SwitchView(ViewState.SaveManage);
        CreateWorldButton.Pressed += () => SwitchView(ViewState.CreateWorld);
        SearchRoomButton.Pressed += () => SwitchView(ViewState.RoomInfo);
    }


    /// <summary>注：回调函数-选中某个世界存档</summary>
    private void OnSelectWorldSave(SplitButtonUI ui)
    {
        if (!WorldManager.Instance.IsValidWorld(ui.DataID)) return;

        WorldManager.Instance.SelWorldIdx = ui.DataID;
        SwitchView(ViewState.WorldInfo);
    }


    /// <summary>注：回调函数-删除某个世界存档</summary>
    private void OnDeleteWorldSave(SplitButtonUI ui)
    {
        if (!WorldManager.Instance.RemoveWorldData(ui.DataID)) return;

        ui.QueueFree();
        SwitchView(ViewState.SaveManage);
    }
}
