using Godot;
using 维修公司.Dll.Interface;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Comp;


namespace 途畔归所.Dll.Creature.Npc
{

    public partial class Npc : Humanoid, IInteractable
    {

        // 便捷属性
        public float m_PatrolRadius => m_CreatureData.PatrolRadius;                 // 巡逻半径
        public float m_PatrolStopTime => m_CreatureData.PatrolStopTime;             // 巡逻点停留时间
        public float m_ChaseTargetDistance => m_CreatureData.ChaseTargetDistance;     // 追击时与目标保持的距离
        public float m_RotationSpeed => m_CreatureData.RotationSpeed;              // 转身速度

        public string ObjectName => m_CreatureData.Name;

        // 私有组件
        public SenseComp m_SenseComp;
        public NpcMovement m_NpcMovement;
        private NpcAI m_NpcAI;
        public NpcBattle m_NpcBattle;
        public DialogComp m_DialogComp;

        public override void _Ready()
        {
            base._Ready();

            m_NpcBattle ??= new NpcBattle();
            m_NpcMovement ??= new NpcMovement();
            m_SenseComp ??= new SenseComp();
            m_NpcAI ??= new NpcAI();
            m_DialogComp ??= new DialogComp();

            AddChild(m_NpcBattle);
            AddChild(m_NpcMovement);
            AddChild(m_SenseComp);
            AddChild(m_NpcAI);
            AddChild(m_DialogComp);

            OnHitEvent += Npc_OnHitEvent;
        }

        private void Npc_OnHitEvent(float damage, Node attacker)
        {
            if (damage >= m_StaggerDamage)
            {
                m_StateMachine.RequestStagger();
            }
        }


        /// <summary>注：互动接口实现</summary>
        public void PlayerInteract(bool InputE, bool InputF, CreatureBase creature)
        {
            if (creature is not Player pl) return;

            if (InputE)
            {
                m_DialogComp.StartDialog(pl);
            }
        }
    }

}
