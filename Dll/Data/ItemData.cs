using Godot;
using Godot.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using 维修公司.Utils;
using 途畔归所.Dll.Core;
using static ItemComp;

namespace 维修公司.Dll.data
{
	[GlobalClass]
	public partial class ItemData : Resource
	{

		public enum ItemType
		{
			消耗品,
			工具,
			收纳
		}
		
		[Export] public string m_ID { get; set; }
		[Export] public string m_Name { get; set; } = string.Empty;
		[Export] public ItemType m_Type { get; set; }
		[Export] public string m_Description { get; set; }
		[Export] public Texture2D m_Icon { get; set; }
		[Export] public int m_Stack { get; set; } = 1;
		[Export] public int m_MaxStack { get; set; } = 1;
		[Export] public float m_Weight { get; set; } = 1f;
		[Export] public int m_Volume { get; set; } = 1;
		[Export] public int m_Capacity { get; set; } = 1;
		[Export] public int m_MaxCapacity { get; set; } = 1;

		public ItemData CopyData()
		{

			return new ItemData()
			{
				m_ID = this.m_ID,
				m_Name = this.m_Name,
				m_Type = this.m_Type,
				m_Description = this.m_Description,
				m_Icon = this.m_Icon,
				m_Stack = this.m_Stack,
				m_MaxStack = this.m_MaxStack,
				m_Weight = this.m_Weight
			};
		}



		public RigidBody3D DataToDrop()
		{
			var drop = ItemManager.Instance.GetItemDrop(this.m_ID);
			ToolUtils.GetNodeScript<ItemComp>(drop).m_ItemData = this;
			return drop;
		}

		public bool IsStack() => m_Stack < m_MaxStack;

		public int GetStackNum() => Mathf.Max(0, m_MaxStack - m_Stack);

		public bool TryStack(ItemData outData)
		{
			if (outData == null) return false;
			if (outData.m_ID != m_ID) return false;
			if (!IsStack()) return false;

			while (IsStack() && outData.m_Stack > 0)
			{
				m_Stack++;
				outData.m_Stack--;
			}

			return outData.m_Stack <= 0;
		}





	}




}
