using Godot;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Creature.StateMachine;

namespace 途畔归所.Dll.Creature.Npc
{
    [GlobalClass]
    public partial class NpcAI : Node
    {
        private Npc m_Npc;
        private NpcMovement m_Movement;

        public CreatureBase m_huntTarget;

        // 巡逻停留计时
        private float m_StopTimer;
        private bool m_isWaiting = false;
        private Vector3 _lastChaseTarget = Vector3.Zero;
        public override void _Ready()
        {
            if (NetCore.Instance.IsClient)
            {
                CatUtils.StopAndExit(this);
                return;
            }

            if (GetParent() is not Npc comp)
            {
                CatLog.Warn("[NpcAI._Ready] 挂载的对象不是 Npc");
                CatUtils.StopAndExit(this);
                return;
            }
            m_Npc = comp;

            m_Movement = CatUtils.FindChildNode<NpcMovement>(m_Npc);

            if ( m_Movement == null)
            {
                CatLog.Err("[NpcAI._Ready] 未挂载重要组件");
                CatUtils.StopAndExit(this);
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            float dt = (float)delta;

            See();

            switch (m_Npc.m_NpcState)
            {
                case NpcState.Patrol:
                    UpdatePatrol(dt);
                    break;
                case NpcState.Chase:
                    UpdateChase();
                    break;
            }
        }


        /// <summary>注：视觉 </summary>
        private void See()
        {
            if (m_Npc.m_Eye.IsColliding() == false || m_huntTarget != null) return;


            var collider = m_Npc.m_Eye.GetCollider();
            if (collider is not CreatureBase creature) return;


            m_huntTarget = creature;
            m_Npc.m_NpcState = NpcState.Chase;
            GD.Print("测试:发现玩家辣！");
        }


        /// <summary>注：巡逻决策 </summary>
        private void UpdatePatrol(float delta)
        {
            // 状态自检
            if (m_Npc.m_NpcState != NpcState.Patrol)
            {
                m_isWaiting = false;
                m_StopTimer = 0f;
                m_Movement.ClearNavigation();
                return;
            }

            if (m_isWaiting)
            {
                m_StopTimer -= delta;
                if (m_StopTimer <= 0f)
                {
                    m_isWaiting = false;
                    GenerateNavPatrolTarget();
                }
                return;
            }

            if (m_Movement.m_navAgent.IsNavigationFinished())
            {
                m_StopTimer = m_Npc.m_PatrolStopTime;
                m_isWaiting = true;
            }
        }


        /// <summary>注：追击导航模式 </summary>
        private void UpdateChase()
        {
            if (m_Npc.m_NpcState != NpcState.Chase) return;


            if (m_huntTarget == null || !IsInstanceValid(m_huntTarget))
            {
                m_huntTarget = null;
                m_Npc.m_NpcState = NpcState.Patrol;
                m_Movement.ClearNavigation();
                _lastChaseTarget = Vector3.Zero;
                return;
            }

            // 1. 将玩家位置吸附到导航网格，避免路径抖动
            Rid map = m_Movement.m_navAgent.GetNavigationMap();
            Vector3 targetOnNav = NavigationServer3D.MapGetClosestPoint(map, m_huntTarget.GlobalPosition);

            // 2. 距离阈值判断（第一次追击或玩家移动超过 0.5m 才更新）
            if (_lastChaseTarget == Vector3.Zero || targetOnNav.DistanceSquaredTo(_lastChaseTarget) > 0.25f)
            {
                m_Movement.SetNavigation(targetOnNav);
                _lastChaseTarget = targetOnNav;
            }
        }

        /// <summary>注：生成巡逻导航点 </summary>
        private void GenerateNavPatrolTarget()
        {
            if (m_Movement.m_navAgent == null) return;

            Vector3 origin = m_Npc.GlobalPosition;
            float radius = m_Npc.m_PatrolRadius;
            int maxAttempts = 15;

            for (int i = 0; i < maxAttempts; i++)
            {
                float angle = (float)GD.RandRange(0, Mathf.Pi * 2);
                float dist = (float)GD.RandRange(1.0f, radius);
                Vector3 candidate = origin + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * dist;

                Rid map = m_Movement.m_navAgent.GetNavigationMap();
                Vector3 closest = NavigationServer3D.MapGetClosestPoint(map, candidate);

                float d = closest.DistanceTo(origin);
                if (d <= radius && d > m_Npc.m_ChaseTargetDistance * 1.5f)
                {
                    m_Movement.SetNavigation(closest);
                    return;
                }
            }

            m_StopTimer = m_Npc.m_PatrolStopTime;
            m_isWaiting = true;
            CatLog.Warn("[NpcAI] 未找到合适巡逻点，原地停留");
        }
    }
}