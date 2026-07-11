using Godot;
using Godot.Collections;
using 维修公司.Dll.data;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using static 维修公司.Dll.data.ItemData;

namespace 途畔归所.Dll.Creature
{
    public partial class Equipment : Node
    {
        private Humanoid m_Humanoid;
        private StateMachine m_StateMachine;
        private Array<ItemData> m_EquipData { get => m_Humanoid.m_EquipData; set => m_Humanoid.m_EquipData = value; }
        public ItemData m_WeaponData { get => GetEquipData("Weapon"); set => SetEquipData("Weapon", value); }

        private ItemComp m_Unarmed;
        private ItemComp m_WeaponComp;





        public override void _EnterTree()
        {
            // 初始化
            if (GetParent() is not Humanoid humanoid)
            {
                CatUtils.StopAndExit(this);
                CatLog.Err($"[Equipment._Ready]：父对象不是 Humanoid 类，已销毁");
                return;
            }
            m_Humanoid = humanoid;
            m_StateMachine = humanoid.m_StateMachine;

            CatLog.Ok($"[Equipment._EnterTree]：{m_Humanoid.m_CreatureData.Name}开始执行");

            InitEquipData();
            LoadUnarmed();
            UpdateWeapon();
        }

        /// <summary>辅助：加载 人形生物 默认攻击件</summary>
        private void LoadUnarmed()
        {
            m_Unarmed ??= ItemManager.Instance.GetItemDrop("7at_空拳头");
            m_Unarmed.SetEquip();
            if (m_Unarmed == null) CatLog.Warn("[Equipment._Ready] 人形生物 的拳头item 未有加载成功");
        }

        public void UpdateWeapon()
        {
            // 1. 无武器数据，空手已经在手上 -> 只需卸掉任何残留的真武器
            if (m_WeaponData == null && m_Unarmed.IsInsideTree())
            {
                if (m_WeaponComp != null)
                {
                    m_WeaponComp.UnbindAnim(m_Humanoid.m_AnimComp);
                    m_WeaponComp.QueueFree();
                    m_WeaponComp = null;
                }
                return;
            }

            // 2. 无武器数据，空手还没挂上 -> 卸掉真武器，挂上空手
            if (m_WeaponData == null && !m_Unarmed.IsInsideTree())
            {
                if (m_WeaponComp != null)
                {
                    m_WeaponComp.UnbindAnim(m_Humanoid.m_AnimComp);
                    m_WeaponComp.QueueFree();
                    m_WeaponComp = null;
                }
                m_Humanoid.m_HandR.AddChild(m_Unarmed);
                m_Unarmed.BindAnim(m_Humanoid.m_AnimComp);
                m_StateMachine.SwitchAttackAnimIndex(m_Unarmed.m_ItemData.AttackAnimIndex);
                return;
            }

            // 3. 有武器数据，空手还挂着，且没实例化真武器 -> 销毁空手，生成真武器
            if (m_WeaponData != null && m_Unarmed.IsInsideTree() && m_WeaponComp == null)
            {
                // 移除空手
                m_Unarmed.UnbindAnim(m_Humanoid.m_AnimComp);
                m_Humanoid.m_HandR.RemoveChild(m_Unarmed);

                // 实例化真武器
                var newWeapon = m_WeaponData.DataToDrop();
                if (newWeapon == null) return;

                newWeapon.SetEquip();
                m_Humanoid.m_HandR.AddChild(newWeapon);
                newWeapon.BindAnim(m_Humanoid.m_AnimComp);
                m_WeaponComp = newWeapon;
                m_StateMachine.SwitchAttackAnimIndex(newWeapon.m_ItemData.AttackAnimIndex);
                return;
            }

            // 4. 武器切换（已有真武器，但数据变了，且不是同一个武器）
            if (m_WeaponData != null && m_WeaponComp != null && m_WeaponData.ID != m_WeaponComp.m_ItemData.ID)
            {
                // 卸掉旧武器
                m_WeaponComp.UnbindAnim(m_Humanoid.m_AnimComp);
                m_WeaponComp.QueueFree();
                m_WeaponComp = null;

                // 生成新武器
                var newWeapon = m_WeaponData.DataToDrop();
                if (newWeapon == null) return;
                newWeapon.SetEquip();
                m_Humanoid.m_HandR.AddChild(newWeapon);
                newWeapon.BindAnim(m_Humanoid.m_AnimComp);
                m_WeaponComp = newWeapon;
                m_StateMachine.SwitchAttackAnimIndex(newWeapon.m_ItemData.AttackAnimIndex);
            }
        }





        /// <summary>注：初始化装备数据 </summary>
        private void InitEquipData()
        {
            m_EquipData ??= [];

            if (m_EquipData.Count == 0)
            {
                m_EquipData.Add(null);
            }

            if (m_EquipData.Count > 1)
            {
                for (int i = m_EquipData.Count - 1; i > 0; i--)
                {
                    m_EquipData.RemoveAt(i);
                }
            }
        }

        private ItemData GetEquipData(string equip)
        {
            InitEquipData();
            if (equip == "Weapon")
            {
                if (m_EquipData[0]?.Type != E_ItemType.Weapon)
                {
                    m_EquipData[0] = null;
                }
                return m_EquipData[0];
            }

            return null;
        }

        private void SetEquipData(string equip, ItemData data)
        {
            InitEquipData();
            if (equip == "Weapon")
            {
                m_EquipData[0] = data;
                UpdateWeapon();
            }
        }

    }
}
