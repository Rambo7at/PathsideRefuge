using Godot;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.View;

/// <summary>注：主菜单视图，负责主界面的按钮交互与界面切换</summary>
public partial class MainMenuView : Control
{
    /// <summary>注：游玩菜单视图（大厅菜单）</summary>
    [Export] private Control PlayMenuView;

    /// <summary>注：开始游戏按钮</summary>
    [Export] private Button StartButton;

    /// <summary>注：设置按钮</summary>
    [Export] private Button SettingsButton;

    /// <summary>注：退出游戏按钮</summary>
    [Export] private Button QuitButton;


    /// <summary>注：节点就绪时进行安全检查、绑定信号并初始化界面</summary>
    public override void _Ready()
    {
        if (PlayMenuView == null || StartButton == null || SettingsButton == null || QuitButton == null)
        {
            CatLog.Err("[MainMenuView._Ready]：缺少节点绑定，请检查 Inspector 面板中的导出属性。");
            return;
        }

        PlayMenuView.Visible = false;


        StartButton.Pressed += StartGame;
        SettingsButton.Pressed += OpenSettings;
        QuitButton.Pressed += QuitGame;

        Visible = true;
    }


    /// <summary>注：回调函数-进入大厅菜单</summary>
    private void StartGame()
    {
        Visible = false;
        PlayMenuView.Visible = true;
    }


    /// <summary>注：回调函数-打开设置菜单</summary>
    private void OpenSettings()
    {
        CatLog.Ok("[MainMenuView]：打开设置菜单");
        // 待实现：打开设置面板的逻辑
    }


    /// <summary>注：回调函数-退出游戏</summary>
    public void QuitGame() => GetTree().Quit();
}
