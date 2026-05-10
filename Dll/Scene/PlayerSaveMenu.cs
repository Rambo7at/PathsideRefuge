using Godot;
using Godot.Collections;
using System;
using 维修公司.Dll;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Manager;

public partial class PlayerSaveMenu : Control
{


	[Export] Label PlayerName;

	[Export] Label PlayerBox;

	[Export] Button SavePice;

	[Export] VBoxContainer SaveBox;

	[Export] VBoxContainer PlayerDataInfo;

	private Array<PlayerSaveSlotComp> SaveBoxArray = [];

	string Pickinfo;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Pickinfo = SavePice.Text;

		SaveBox.Visible = false;

		PlayerDataInfo.Visible = true;

		RefreshSaveBox();

	}


	public override void _Process(double delta)
	{
		PlayerData playerData = SaveManager.Instance.GetPickPlayerData();

		if (playerData == null) return;

		ApplyPlayerInfo(playerData);

		PlayerManager.Instance.m_LocalPlayerData = playerData.DeepCopy();
	}







	/// <summary>回调函数：进入存档选择界面</summary>
	private void OpenSaveSelection()
	{
		SaveBox.Visible = !SaveBox.Visible;
		PlayerDataInfo.Visible = !PlayerDataInfo.Visible;

		if (SaveBox.Visible == true) SavePice.Text = "返回";
		else SavePice.Text = Pickinfo;

	}

	/// <summary>回调函数：进入创建界面</summary>
	private void Creator()
	{
		GetTree().ChangeSceneToFile("res://Scenes/角色创建.tscn");
	}


	private void ApplyPlayerInfo(PlayerData playerData)
	{
		if (playerData == null) return;

		PlayerName.Text = "玩家名：" + playerData.m_Name;

		PlayerBox.Text = "背包库存：" + playerData.GetInventoryItemCount();
	}

	/// <summary>
	/// 注：刷新存档格子
	/// </summary>
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

		var IDs = SaveManager.Instance.GetPlayerIDList();

		if (IDs.Count <= 1) return;

		for (int i = 0; i < IDs.Count; i++)
		{
			var ui = UIManager.Instance.GetUI("存档信息") as PlayerSaveSlotComp;

			if (ui == null) return;

			ui.m_PlayerID = IDs[i];

			SaveBox.AddChild(ui);
		}
	}


}
