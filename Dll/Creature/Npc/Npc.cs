using Godot;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Comp;


namespace 途畔归所.Dll.Creature.Npc
{

    public partial class Npc : Humanoid
    {

        // 便捷属性
        public float m_PatrolRadius => m_CreatureData.PatrolRadius;                 // 巡逻半径
        public float m_PatrolStopTime => m_CreatureData.PatrolStopTime;             // 巡逻点停留时间
        public float m_ChaseTargetDistance => m_CreatureData.ChaseTargetDistance;     // 追击时与目标保持的距离
        public float m_RotationSpeed => m_CreatureData.RotationSpeed;              // 转身速度

        // 私有组件
        public SenseComp m_SenseComp;
        public NpcMovement m_NpcMovement;
        private NpcAI m_NpcAI;
        public NpcBattle m_NpcBattle;

        public override void _Ready()
        {
            base._Ready();

            AddChild(m_NpcBattle ??= new NpcBattle());
            AddChild(m_NpcMovement ??= new NpcMovement());
            AddChild(m_SenseComp ??= new SenseComp());
            AddChild(m_NpcAI ??= new NpcAI());

            OnHitEvent += Npc_OnHitEvent;
        }

        private void Npc_OnHitEvent(float damage, Node attacker)
        {
            if (damage >= m_StaggerDamage)
            {
                m_StateMachine.RequestStagger();
            }
        }


    }

}
