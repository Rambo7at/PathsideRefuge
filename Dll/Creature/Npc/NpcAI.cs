using Godot;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Comp;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Creature.StateMachine;

namespace 途畔归所.Dll.Creature.Npc
{
	public partial class NpcAI : Node
	{
		// 组件
		private Npc m_Npc;
		private NpcMovement m_Movement;
		private StateMachine m_StateMachine;
		private SenseComp m_SenseComp;
		private Equipment m_Equipment;
		private NpcBattle m_NpcBattle;



		public CreatureBase m_huntTarget;

		// 巡逻停留计时
		private float m_StopTimer;
		private bool m_IsWaiting = false;
		private Vector3 _lastChaseTarget = Vector3.Zero;


		// 便捷属性
		private float PatrolStopTime => m_Npc.m_CreatureData.PatrolStopTime;
		private float PatrolRadius => m_Npc.m_CreatureData.PatrolRadius;

		private bool IsPatrol => m_StateMachine.m_NpcState == NpcState.Patrol;
		private bool IsChase => m_StateMachine.m_NpcState == NpcState.Chase;

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
			m_StateMachine = comp.m_StateMachine;
			m_Movement = comp.m_NpcMovement;
			m_SenseComp = comp.m_SenseComp;
			m_NpcBattle = comp.m_NpcBattle;

			if (m_Movement == null)
			{
				CatLog.Err("[NpcAI._Ready] 未挂载重要组件");
				CatUtils.StopAndExit(this);
			}
		}

		public override void _PhysicsProcess(double delta)
		{
			float dt = (float)delta;

			SenseAI();

			switch (m_StateMachine.m_NpcState)
			{
				case StateMachine.NpcState.Patrol:
					UpdatePatrol(dt);
					break;
				case StateMachine.NpcState.Chase:
					UpdateChase();
					break;
			}
		}


		/// <summary>注：感知 </summary>
		private void SenseAI()
		{
			if (m_SenseComp.m_DetectedCreaturesList.Count == 0) return;
			if (IsChase) return;

			foreach (var target in m_SenseComp.m_DetectedCreaturesList)
			{
				if (target == null) continue;

				m_huntTarget = target;
				m_StateMachine.SwitchNpcState(NpcState.Chase);
			}

			if (m_huntTarget != null) GD.Print("测试:发现玩家辣！");
		}


		/// <summary>注：巡逻决策 </summary>
		private void UpdatePatrol(float delta)
		{
			if (m_StateMachine.m_NpcState != NpcState.Patrol)
			{
				m_IsWaiting = false;
				m_StopTimer = 0f;
				m_Movement.ClearNavigation();
				return;
			}

			if (m_IsWaiting)
			{
				m_StopTimer -= delta;
				if (m_StopTimer <= 0f)
				{
					m_IsWaiting = false;
					m_Movement.SetRandomPatrolTarget(m_Npc.m_PatrolRadius, m_Npc.m_ChaseTargetDistance * 1.5f);
				}
				return;
			}

			if (m_Movement.IsNavigationFinished())
			{
				m_StopTimer = PatrolStopTime;
				m_IsWaiting = true;
			}
		}


		/// <summary>注：追击导航模式 </summary>
		private void UpdateChase()
		{
			if (!IsChase) return;

			if (m_huntTarget == null || !IsInstanceValid(m_huntTarget))
			{
				StopChase();
				return;
			}

			float dist = m_Npc.GlobalPosition.DistanceTo(m_huntTarget.GlobalPosition);

			if (dist <= 2 && m_StateMachine.m_AnimState != StateMachine.AnimState.Attack)
			{
				if (m_NpcBattle == null)
				{
					CatLog.Debug("m_NpcBattle 是空的");
					return;
				}
				m_NpcBattle.attack();
			}
			else
			{
				m_Movement.SetNavigation(m_huntTarget.GlobalPosition);
			}
		}

		private void StopChase()
		{
			m_huntTarget = null;
			m_StateMachine.SwitchNpcState(NpcState.Patrol);
			m_Movement.ClearNavigation();
		}


	}
}
