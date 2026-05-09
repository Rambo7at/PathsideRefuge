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
			if (NetCore.Instance.IsHost)
			{
				PlayerManager.Instance.SpawnLocalPlayer(SpawnPian.GlobalPosition);
			}
			else
			{
                

            }
        }
		


		public override void _Process(double delta)
		{

		}
	}
}
