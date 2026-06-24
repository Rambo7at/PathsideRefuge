using Godot;
using Godot.Collections;
using 维修公司.Dll.data;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Data;

namespace 途畔归所.Dll.Base
{
	public partial class Humanoid : CreatureBase
	{
		[Export] public BoneAttachment3D m_HandL;
		[Export] public BoneAttachment3D m_HandR;


		public Equipment m_Equipment;
		public float m_EquipAttack => m_Equipment.m_Weapon != null ? m_Equipment.m_Weapon.Damage : 0f;
		public Area3D m_EquipHitBox => m_Equipment.m_WeaponComp != null ? m_Equipment.m_WeaponComp.m_WeaponHitBox : null;
		public InventoryData m_InventoryData { get => m_CreatureData.InventoryData; set => m_CreatureData.InventoryData = value; }
		public Array<ItemData> m_EquipData { get => m_CreatureData.EquipData; set => m_CreatureData.EquipData = value; }

		public override void _EnterTree()
		{
			base._EnterTree();

			m_Equipment ??= new Equipment();
			AddChild(m_Equipment);


		}


		public override void _Ready()
		{
			base._Ready();



		}



		protected override float FinalDamage()
		{
			float damage = m_BaseDamage;
			damage += m_EquipAttack;

			return damage;
		}




	}
}
