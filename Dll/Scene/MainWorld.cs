using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Core;

namespace 途畔归所.Dll.Scene
{
	public partial class MainWorld : Node3D
	{
		[Export] public Node3D SpawnPian;

		public override void _Ready()
		{
			var pl =   GameCore.Instance.m_PlayerManager.GetPlyaer();
			if (pl == null) return;

			if (!pl.IsInsideTree())
			{
				GetTree().CurrentScene.AddChild(pl);
			}

			pl.GlobalPosition = SpawnPian.GlobalPosition;
		}


		public override void _Process(double delta)
		{

		}
	}
}
