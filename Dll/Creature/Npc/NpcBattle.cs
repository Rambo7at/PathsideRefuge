using Godot;
using System.Collections.Generic;
using 维修公司.Dll.data;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Creature.StateMachine;

namespace 途畔归所.Dll.Creature.Npc
{
    public partial class NpcBattle : Node
    {

        public class AttackInfo
        {
            public ItemComp item;

            public bool IsReady => interval <= 0;

            public double interval = 0;
            public AttackInfo(ItemComp comp) => item = comp;

            public void StartCooldown() => interval = item.m_ItemData.AttackInterval;

            public void Cooldown(double delta) => interval -= delta;

        }


        private Npc m_Npc;
        private Equipment m_Equipment => m_Npc?.m_Equipment;
        private StateMachine m_StateMachine => m_Npc?.m_StateMachine;
        private List<AttackInfo> m_AttackInfos = [];

        private AnimState AnimState => m_StateMachine.m_AnimState;
        private bool IsAttackState => AnimState == AnimState.Attack;


        public override void _Ready()
        {
            if (GetParent() is not Npc node)
            {
                CatUtils.StopAndExit(this);
                return;
            }
            m_Npc = node;

            if (m_Npc.m_AttackItems.Count == 0) return;

            foreach (var item in m_Npc.m_AttackItems)
            {
                if (item?.m_ItemType != ItemData.E_ItemType.Weapon) continue;
                m_AttackInfos.Add(new(item));
            }

            m_Equipment.m_WeaponData = m_AttackInfos[0].item.m_ItemData;
        }

        public override void _PhysicsProcess(double delta)
        {
            CooldownAttackInfos(delta);
        }


        public void attack()
        {
            if (IsAttackState || m_Npc.IsDead) return;

            var info = GetReadyAttack();
            if (info == null) return;

            m_Equipment.m_WeaponData = info.item.m_ItemData; // 装上hitbox

            m_StateMachine.RequestAttack();

            info.StartCooldown();
        }


        private AttackInfo GetReadyAttack()
        {
            AttackInfo info = null;

            foreach (var item in m_AttackInfos)
            {
                if (item.IsReady)
                {
                    info = item;
                    break;
                }
            }

            return info;
        }


        private void CooldownAttackInfos(double delta)
        {
            foreach (var item in m_AttackInfos)
            {
                if (item.IsReady) continue;

                item.Cooldown(delta);
            }
        }

    }
}
