using Godot;
using System;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Manager;

public partial class PlayerCreator : SceneBase
{
    [Export] private LineEdit m_Name;




	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}


	#region 回调函数

	private void Return() => GetTree().ChangeSceneToFile("res://Scenes/主菜单.tscn");

	private void Creator()
	{
		GD.Print(m_Name.Text);

		if (!string.IsNullOrEmpty(m_Name.Text))
		{
			SaveManager.Instance.CreatePlayer(m_Name.Text);
			Return();
		}
	}


	#endregion


}
