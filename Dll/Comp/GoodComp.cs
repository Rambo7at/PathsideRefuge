using Godot;
using System;

public partial class GoodComp : Control
{

	[Export] public TextureRect 图片栏;

	[Export] public Label 价格栏;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
