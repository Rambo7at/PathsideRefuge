using Godot;
using System;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Manager;

public partial class PlayerSaveMenu : Control
{


	[Export] Label PlayerName;

	[Export] Label PlayerBox;

    [Export] Button b_Left;

    [Export] Button b_Right;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (SaveManager.Instance.DATA == null) return;

        PlayerData playerData = SaveManager.Instance.DATA.GetPickPlayerData();

        if (playerData == null) return;

        ApplyPlayerInfo(playerData);
    }



    #region 回调函数

    private void GoLeft()
    { 
    
    
    }

    private void GoRight() => SaveManager.Instance.DATA.PickNextPlayer();

    #endregion


    private void ApplyPlayerInfo(PlayerData playerData)
	{
		if (playerData == null) return;

        PlayerName.Text = "玩家名：" + playerData.m_Name;

        if (playerData.m_InventoryData == null || playerData.m_InventoryData.Count == 0)
        {
            PlayerBox.Text = "背包库存：0";
        }
        else
        {
            PlayerBox.Text = "背包库存：" + playerData.m_InventoryData.Count;
        }
    }



}
