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
		if (m_MainMenuUI == null) GD.PrintErr("[MainMenu]：初始化时检测[m_MainMenuUI]是空的");
		if (m_StartGameUI == null) GD.PrintErr("[MainMenu]：初始化时检测[m_StartGameMeun]是空的");
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
	
	
	}


	public void CreateLobby()
	{ 
	
	
	
	}


	#endregion


}
