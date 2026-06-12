using Godot;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Creature.Npc.NpcStateMachine;

namespace 途畔归所.Dll.Creature.Npc
{
    [GlobalClass]
    public partial class NpcAI : Node
    {
        private Npc _npc;
        private NpcStateMachine _stateMachine;
        private NpcMovement _movement;
        private CreatureData _npcData;

        public CreatureBase m_huntTarget;
        
        // 巡逻停留计时
        private float m_navStopTimer = 0f;
        private bool m_isWaiting = false;
        private bool m_isHunt = false;
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
            _npc = comp;
            _npcData = _npc.m_data ?? new CreatureData();

            _stateMachine = CatUtils.FindChildNode<NpcStateMachine>(_npc);
            _movement = CatUtils.FindChildNode<NpcMovement>(_npc);

            if (_stateMachine == null || _movement == null)
            {
                CatLog.Err("[NpcAI._Ready] 未挂载重要组件");
                CatUtils.StopAndExit(this);
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            float dt = (float)delta;

            See();

            switch (_stateMachine.m_npcState)
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
            if (_npc.m_eye.IsColliding() == false || m_huntTarget != null)
                return;

            var collider = _npc.m_eye.GetCollider();
            if (collider is not Player pl)
                return;

            m_huntTarget = pl;
            _stateMachine.m_npcState = NpcState.Chase;
            GD.Print("测试:发现玩家辣！");
        }


        /// <summary>注：巡逻决策 </summary>
        private void UpdatePatrol(float delta)
        {
            // 状态自检
            if (_stateMachine.m_npcState != NpcState.Patrol)
            {
                m_isWaiting = false;
                m_navStopTimer = 0f;
                _movement.ClearNavigation();
                return;
            }

            if (m_isWaiting)
            {
                m_navStopTimer -= delta;
                if (m_navStopTimer <= 0f)
                {
                    m_isWaiting = false;
                    GenerateNavPatrolTarget();
                }
                return;
            }

            if (_movement.m_navAgent.IsNavigationFinished())
            {
                m_navStopTimer = _npcData.m_patrolStopTime;
                m_isWaiting = true;
            }
        }


        /// <summary>注：追击导航模式 </summary>
        private void UpdateChase()
        {
            if (_stateMachine.m_npcState != NpcState.Chase)
                return;

            if (m_huntTarget == null || !IsInstanceValid(m_huntTarget))
            {
                m_huntTarget = null;
                _stateMachine.m_npcState = NpcState.Patrol;
                _movement.ClearNavigation();
                _lastChaseTarget = Vector3.Zero;
                return;
            }

            // 1. 将玩家位置吸附到导航网格，避免路径抖动
            Rid map = _movement.m_navAgent.GetNavigationMap();
            Vector3 targetOnNav = NavigationServer3D.MapGetClosestPoint(map, m_huntTarget.GlobalPosition);

            // 2. 距离阈值判断（第一次追击或玩家移动超过 0.5m 才更新）
            if (_lastChaseTarget == Vector3.Zero || targetOnNav.DistanceSquaredTo(_lastChaseTarget) > 0.25f)
            {
                _movement.SetNavigation(targetOnNav);
                _lastChaseTarget = targetOnNav;
            }
        }

        /// <summary>注：生成巡逻导航点 </summary>
        private void GenerateNavPatrolTarget()
        {
            if (_movement.m_navAgent == null) return;

            Vector3 origin = _npc.GlobalPosition;
            float radius = _npcData.m_patrolRadius;
            int maxAttempts = 15;

            for (int i = 0; i < maxAttempts; i++)
            {
                float angle = (float)GD.RandRange(0, Mathf.Pi * 2);
                float dist = (float)GD.RandRange(1.0f, radius);
                Vector3 candidate = origin + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * dist;

                Rid map = _movement.m_navAgent.GetNavigationMap();
                Vector3 closest = NavigationServer3D.MapGetClosestPoint(map, candidate);

                float d = closest.DistanceTo(origin);
                if (d <= radius && d > _npcData.m_chaseTargetDistance * 1.5f)
                {
                    _movement.SetNavigation(closest);
                    return;
                }
            }

            m_navStopTimer = _npcData.m_patrolStopTime;
            m_isWaiting = true;
            CatLog.Warn("[NpcAI] 未找到合适巡逻点，原地停留");
        }
    }
}