using Godot;
using System;
using 维修公司.Dll.data;

public partial class PlacedComp : StaticBody3D
{

	[Export] public PlacedData m_PlacedData { get; set; }


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
