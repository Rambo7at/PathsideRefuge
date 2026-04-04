using Godot;
using Godot.Collections;
using System;
using System.Runtime.InteropServices.JavaScript;
using 维修公司.Dll;
using 途畔归所.Dll.Core;

public partial class MainMenu : Node3D
{

	[Export]
	public Array<PackedScene> 物件资源 = new Array<PackedScene>();

	[Export] private Control m_MainMenuUI;

	[Export] private Control m_StartGameUI;


	public override void _Ready()
	{
		PieceManager.Instance.InitPieceManager(物件资源);
		if (m_MainMenuUI == null) GD.PrintErr("[MainMenu]：初始化检测[m_MainMenuUI]是空的");
		if (m_StartGameUI == null) GD.PrintErr("[MainMenu]：初始化检测[m_StartGameMeun]是空的");
	}


	public override void _Process(double delta)
	{

	}


	#region 主菜单_画布-按钮信号
	/// <summary>注：加载游戏场景</summary>
	public void NewGame()
	{
		m_MainMenuUI.Visible = false;
		m_StartGameUI.Visible = true;
	}


	/// <summary>注：退出游戏</summary>
	public void Quit() => GetTree().Quit();
	#endregion

	#region  大厅界面-按钮信号

	public void Return()
	{
		m_MainMenuUI.Visible = true;
		m_StartGameUI.Visible = false;
	}

	public void LocalGame()
	{

		if (CheckPlayerSave() == false) return;
		GetTree().ChangeSceneToFile("res://Scenes/测试场景.tscn");
	}


	public void CreateLobby()
	{
        if (CheckPlayerSave() == false) return;
		GameCore.Instance.m_NetworkCore.StartLANHost();

    }

	public void JoinLobby()
	{
        if (CheckPlayerSave() == false) return;
        GameCore.Instance.m_NetworkCore.JoinLAN("192.168.71.36");


    }


    #endregion

    #region 辅助方法
    private bool CheckPlayerSave()
	{

        var playerdata = GameCore.Instance.m_PlayerManager.GetLocalPlayerData();

        if (playerdata == null)
        {
            GD.Print("未检测到本地存档数据");
            GetTree().ChangeSceneToFile("res://Scenes/角色创建.tscn");
            return false;
        }
		return true;
    }





    #endregion

}
