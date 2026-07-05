using Godot;
using Godot.Collections;
using 维修公司.Dll.data;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Creature
{
	public partial class Equipment : Node
	{

		private Humanoid m_Humanoid;
		private Array<ItemData> m_EquipData => m_Humanoid.m_EquipData;
		private BoneAttachment3D m_HandL => m_Humanoid.m_HandL;
		private BoneAttachment3D m_HandR => m_Humanoid.m_HandR;


		private ItemComp Unarmed;



		public ItemData m_WeaponData => m_EquipData.Count > 0 ? m_EquipData[0] : null;

		public ItemComp m_WeaponComp;



		public override void _Ready()
		{
			// 初始化
			if (GetParent() is not Humanoid humanoid)
			{
				CatUtils.StopAndExit(this);
				return;
			}

			m_Humanoid = humanoid;

			Unarmed ??= ItemManager.Instance.GetItemDrop("7at_空拳头");
			Unarmed.SetEquip();

			// 自检
			if (Unarmed == null) CatLog.Err("[Equipment._Ready] 人形生物 的拳头item 未有加载成功");

		}

		public override void _Process(double delta)
		{
			if (m_WeaponData == null && m_WeaponComp == null)
			{
				m_WeaponComp = Unarmed;
				m_HandR.AddChild(m_WeaponComp);
				m_WeaponComp.BindAnim(m_Humanoid.m_AnimComp);
				m_Humanoid.m_AttackAnimIndex = m_WeaponComp.m_ItemData.AttackAnimIndex;

				return; 
			}

			if (m_WeaponData == null && m_WeaponComp != null) return;
			if (m_WeaponData?.ID == m_WeaponComp?.m_ItemData.ID) return;

			if (m_WeaponData != null && m_WeaponComp == null)
			{
				var DROP = m_WeaponData.DataToDrop();
				DROP.SetEquip();
				m_WeaponComp = DROP;
				m_HandR.AddChild(m_WeaponComp);
				m_WeaponComp.BindAnim(m_Humanoid.m_AnimComp); 
				return;
			}

			if (m_WeaponData == null && m_WeaponComp != null)
			{
				m_WeaponComp.UnbindAnim(m_Humanoid.m_AnimComp); 
				m_WeaponComp.QueueFree();
				m_WeaponComp = null;
				return;
			}
		}




	}
}
