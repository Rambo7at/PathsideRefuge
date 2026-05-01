using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;

namespace 途畔归所.Dll.Scene
{
	public partial class MainWorld : Node3D
	{
		[Export] public Node3D SpawnPian;

        public override void _Ready()
		{
			var pl = PlayerManager.Instance.GetPlyaer();
			if (pl == null) return;

			if (!pl.IsInsideTree())
			{
				GetTree().CurrentScene.AddChild(pl);
                PlayerManager.Instance.ActivePlayers.Add(pl.m_PlayerData.m_PlayerID,pl);
			}

			pl.GlobalPosition = SpawnPian.GlobalPosition;
		}


		public override void _Process(double delta)
		{

		}
	}
}
