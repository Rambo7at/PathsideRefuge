using Godot;
using Godot.Collections;
using 维修公司.Dll.data;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Utils;
using static 维修公司.Dll.data.ItemData;

namespace 途畔归所.Dll.Creature
{
	public partial class Equipment : Node
	{

		private Humanoid m_Humanoid;
		private Array<ItemData> m_EquipData => m_Humanoid.m_EquipData;
		private BoneAttachment3D m_HandL => m_Humanoid.m_HandL;
		private BoneAttachment3D m_HandR => m_Humanoid.m_HandR;


		public ItemData m_Weapon => m_EquipData.Count > 0 ? m_EquipData[0] : null;
		public ItemComp m_WeaponComp;



		public override void _Ready()
		{
			if (GetParent() is not Humanoid humanoid)
			{
				CatUtils.StopAndExit(this);
				return;
			}

			m_Humanoid = humanoid;
		}

		public override void _Process(double delta)
		{
			// 预留注释 -> 如果换上了 同ID 不同属性的武器如何区别
			// 初步构想 全部面板值 + 武器哈希
			if (m_Weapon?.ID == m_WeaponComp?.m_ItemData.ID) return;

			if (m_Weapon != null && m_WeaponComp == null)
			{
				var DROP = m_Weapon.DataToDrop();
				DROP.SetEquip();
				m_WeaponComp = DROP;
				m_HandR.AddChild(m_WeaponComp);
			}

			if (m_Weapon == null && m_WeaponComp != null)
			{
				m_WeaponComp.QueueFree();
				m_WeaponComp = null;
			}
		}




	}
}
