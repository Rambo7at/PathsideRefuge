using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Data;

namespace 途畔归所.Dll.Base
{
	public partial class Humanoid : CreatureBase
	{
		public InventoryData m_InventoryData { get => m_CreatureData.InventoryData; set => m_CreatureData.InventoryData = value; }

		[Export] public BoneAttachment3D m_HandL;
		[Export] public BoneAttachment3D m_HandR;










	}
}
