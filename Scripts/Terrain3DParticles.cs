using Godot;
using System;

public partial class Terrain3DParticles : Node3D
{
	// Called when the node enters the scene tree for the first time.[]


	[Export] public Node3D terrain3D;


	

	public override void _Ready()
	{
		Type type = terrain3D.GetType();
	 

		GD.PrintErr(type.FullName);
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
