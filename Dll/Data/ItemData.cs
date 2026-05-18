using Godot;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using static 维修公司.Dll.data.ItemData;

namespace 维修公司.Dll.data
{
	[GlobalClass]
	public partial class ItemData : Resource
	{
		public enum ItemType
		{
			消耗品 = 0,
			工具 = 1,
			武器 = 2
		}

		public enum WeaponType
		{
			刀 = 0
		}
		public enum AttackType
		{
			消耗品 = 0,
			工具 = 1,
			武器 = 2
		}




		[ExportGroup("基础")]
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



		[ExportGroup("武器")]
		[Export] public WeaponType m_WeaponType { get; set; }
		[Export] public int m_Damage { get; set; }




		public bool m_IsStackable => m_Stack < m_MaxStack;


		public ItemData DeepCopy() => this.DuplicateDeep() as ItemData;

		public RigidBody3D DataToDrop()
		{
			var drop = ItemManager.Instance.GetItemDrop(this.m_ID);
			if (drop is not ItemComp comp) return null;

			comp.m_ItemData = this;
			return drop;
		}

		public int GetStackNum() => Mathf.Max(0, m_MaxStack - m_Stack);

		public bool TryStack(ItemData outData)
		{
			if (outData == null || outData.m_ID != m_ID) return false;

			if (!m_IsStackable) return false;

			while (m_IsStackable && outData.m_Stack > 0)
			{
				m_Stack++;
				outData.m_Stack--;
			}

			return outData.m_Stack <= 0;
		}
	}




}
