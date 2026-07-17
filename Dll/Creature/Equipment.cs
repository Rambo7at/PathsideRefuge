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
        public ItemComp m_WeaponComp;

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


            InitEquipData();
            LoadUnarmed();
            

        }

        public override void _Ready()
        {
            UpdateWeapon();
        }


        /// <summary>辅助：加载 人形生物 默认攻击件</summary>
        private void LoadUnarmed()
        {
            m_Unarmed ??= ItemManager.Instance.GetItemDrop("7at_空拳头");
            m_Unarmed.SetEquip();
            if (m_Unarmed == null) CatLog.Warn("[Equipment._Ready] 人形生物 的拳头item 未有加载成功");
        }

        /// <summary>注：更新装备武器 </summary>
        public void UpdateWeapon()
        {
            // === 卸载当前手上的一切 ===
            if (m_WeaponComp != null)
            {
                m_WeaponComp.UnbindAnim(m_Humanoid);
                m_WeaponComp.QueueFree();
                m_WeaponComp = null;
            }

            if (m_Unarmed.IsInsideTree())
            {
                m_Unarmed.UnbindAnim(m_Humanoid);
                m_Humanoid.m_HandR.RemoveChild(m_Unarmed);
            }

            // === 确定要装什么 ===
            ItemComp targetComp;
            if (m_WeaponData != null)
            {
                // 有武器数据 → 实例化真武器
                targetComp = m_WeaponData.DataToDrop();
                if (targetComp == null) return;
                targetComp.SetEquip();
                m_WeaponComp = targetComp;
            }
            else
            {
                // 无武器数据 → 用空手
                targetComp = m_Unarmed;
            }

            // === 挂载 + 绑定动画 ===
            m_Humanoid.m_HandR.AddChild(targetComp);
            targetComp.BindAnim(m_Humanoid);
            m_StateMachine.SwitchAttackAnimIndex(targetComp.m_AttackAnimIndex);
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
