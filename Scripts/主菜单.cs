using Godot;
using Godot.Collections;
using System;
using 维修公司.Dll;

public partial class 主菜单 : Node3D
{
	[Export]
	public Array<PackedScene> 物品资源 = new Array<PackedScene>();


	[Export]
	public Array<PackedScene> UI资源 = new Array<PackedScene>();


	[Export]
	public Array<PackedScene> 物件资源 = new Array<PackedScene>();

	public override void _Ready()
	{
		UIManager.Instance.InitUIManager(UI资源);
		ItemManager.Instance.InitItemManager(物品资源);
		PieceManager.Instance.InitPieceManager(物件资源);
	}


	public override void _Process(double delta)
	{

	}

	public void NewGame() => GetTree().ChangeSceneToFile("res://Scenes/测试场景.tscn");




}
