using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.NetWork
{

	[GlobalClass]
	public partial class NetSyncBase : Node
	{

	   [Export] private Node3D _node3D;

		public NetObject m_NetObj { get; set; }

		public bool IsOwner => m_NetObj.OwnerPeerID == NetCore.Instance?.LocalPeerID;


		public override void _Ready()
		{
			var node = GetParent();
			if (_node3D == null)
			{
				if (node == null || node is not Node3D node3)
				{
					GD.PrintErr("[NetSyncBase._Ready]：未获取到 Node3D");
					return;
				}

				_node3D = node3;
			}



		

			if (m_NetObj == null)
			{
				// 这里以后会提交 补充注册，就是我手动放置场景内的物品。
				GD.PrintErr("[NetSyncBase._Ready]：发现未注册组件，已提交注册");
			}




		}

		public override void _Process(double delta)
		{

		}


	}
}
