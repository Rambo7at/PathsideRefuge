using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.NetWork
{

	[GlobalClass]
	public partial class NetSyncBase : Node
	{

	    private Node3D _node3D;

		public NetObject m_NetObj { get; set; }

        public bool IsOwner => m_NetObj != null && m_NetObj.OwnerPeerID == NetCore.Instance.LocalPeerID;



        public override void _EnterTree()
        {
            if (m_NetObj == null)
            {

                // 这里以后会提交 补充注册，就是我手动放置场景内的物品。
                CatLog.Warn("[NetSyncBase._Ready]：发现未注册组件，已提交注册");
            }



        }





		public override void _Ready()
		{

            var node = GetParent();

            if (node == null)
            {
                CatLog.Err($"[NetSyncBase._Ready]：检测挂载对象是空，已返回");
                QueueFree();
                return;
            }

            if (node is not Node3D node3D)
            {
                CatLog.Err($"[NetSyncBase._Ready]：检测挂载对象并非 Node3D ，已返回");
                QueueFree();
                return;
            }


            _node3D = node3D;





		}

		public override void _Process(double delta)
		{

		}


	}
}
