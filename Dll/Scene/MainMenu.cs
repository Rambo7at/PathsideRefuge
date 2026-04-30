using Godot;
using Godot.Collections;
using System;
using System.Runtime.InteropServices.JavaScript;
using 维修公司.Dll;
using 途畔归所.Dll.Comp;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;

public partial class MainMenu : Node3D
{

	[Export] private Control 菜单UI;
	[Export] private Control 开始UI;
	[Export] private Control 创建UI;

	[Export] private Button 门牌UI;
	[Export] private LineEdit 房间名称;

	public override void _Ready()
	{
		IsAllFieldsnNull();

		菜单UI.Visible = true;
		开始UI.Visible = false;
		创建UI.Visible = false;
	}


	public override void _Process(double delta)
	{

	   


	}


	#region 主菜单_画布-按钮信号
	/// <summary>注：开始游戏按钮</summary>
	public void StartGame()
	{
		菜单UI.Visible = false;
		开始UI.Visible = true;
	}


	/// <summary>注：退出游戏</summary>
	public void Quit() => GetTree().Quit();
	#endregion





	#region  大厅界面-按钮信号

	/// <summary>注：返回按钮 </summary>
	public void Return()
	{
		菜单UI.Visible = true;
		开始UI.Visible = false;
		创建UI.Visible = false;
		房间名称.Text = "";
	}

	/// <summary>注：本地游戏 </summary>
	public void LocalGame()
	{
		if (SaveManager.Instance.IsValidPlayerSaveData() == false)
		{
			GetTree().ChangeSceneToFile("res://Scenes/角色创建.tscn");
			return;
		}

		GetTree().ChangeSceneToFile("res://Scenes/测试场景.tscn");
	}

	/// <summary>注：在线游戏 </summary>
	public void CreateLobby()
	{
		var X = SaveManager.Instance.GetPickPlayerData();

		if (X == null) return;

		创建UI.Visible = true;

	}

	/// <summary>注：加入游戏 </summary>
	public void JoinLobby()
	{
		NetworkCore.Instance.JoinLAN("192.168.71.36");
	}



	public void GoLobby()
	{
		NetworkCore.Instance.StartLANHost();
		门牌UI.Text = 房间名称.Text;
		创建UI.Visible = false;
		房间名称.Text = "";
	}







	#endregion

	


	



	private bool IsAllFieldsnNull()
	{
		if (菜单UI == null)
		{
			GD.PrintErr("[MainMenu]：初始化字段：[菜单UI]是空");
			return false;
		}
		if (开始UI == null)
		{
			GD.PrintErr("[MainMenu]：初始化字段：[开始UI]是空");
			return false;
		}
		if (创建UI == null)
		{
			GD.PrintErr("[MainMenu]：初始化字段：[创建UI]是空");
			return false;
		}
		if (门牌UI == null)
		{
			GD.PrintErr("[MainMenu]：初始化字段：[门牌UI]是空");
			return false;
		}
		if (房间名称 == null)
		{
			GD.PrintErr("[MainMenu]：初始化字段：[房间名称]是空");
			return false;
		}
		return true;
	}

}
