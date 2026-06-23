using Godot;
using System;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Creature.Npc;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Creature.StateMachine;

namespace 途畔归所.Dll.Creature
{

	[GlobalClass]
	public partial class StateMachine : Node, ISyncStateMachine
	{
		public enum NpcState
		{
			Patrol = 0, // 巡逻
			Chase = 1,  // 追击
		}

		public enum PlayerState
		{
			Idle = 0,
			Interact = 1,
			Build = 2,
		}

		public enum AnimState
		{
			Idle = 0,
			Walk = 1,
			Run = 2,
			Jump = 3,
			Fall = 4,
			Attack = 5,
			Stagger = 6,
			Death = 7,
		}
		public AnimState m_AnimState { get; set; } = AnimState.Idle;
		public NpcState m_NpcState { get; set; } = NpcState.Patrol;
		public PlayerState m_PlayerState { get; set; } = PlayerState.Idle;
		public bool Walk => m_AnimState == AnimState.Walk;
		public bool Jump => m_AnimState == AnimState.Jump;
		public bool Idle => m_AnimState == AnimState.Idle;
		public bool Attack => m_AnimState == AnimState.Attack;
		public bool Stagger => m_AnimState == AnimState.Stagger;
		public bool Death => m_AnimState == AnimState.Death;


		private bool IsOnFloor => m_Creature.IsOnFloor();
		private float Speed => new Vector3(m_Creature.Velocity.X, 0, m_Creature.Velocity.Z).Length();

		private CreatureBase m_Creature;
		private Humanoid m_Humanoid => m_Creature is Humanoid human ? human : null;
		private Player m_Player => m_Humanoid is Player pl ? pl : null;
		private Creature.Npc.Npc m_Npc => m_Humanoid is Creature.Npc.Npc npc ? npc : null;

		private Func<int> OnGetState;

		private Action<int> OnSetState;


		public override void _Ready()
		{
			
			if (GetParent() is not CreatureBase cr)
			{
				CatLog.Err($"[StateMachine._Ready]：检测挂载对象并非 CreatureBase ，已销毁");
				CatUtils.StopAndExit(this);
				return;
			}

			m_Creature = cr;

			if (m_Player != null)
			{
				OnGetState = () => (int)m_PlayerState;
				OnSetState = (index) => m_PlayerState = (PlayerState)index;
			}
			else if (m_Npc != null)
			{
				OnGetState = () => (int)m_NpcState;
				OnSetState = (index) => m_NpcState = (NpcState)index;
			}

			if (OnGetState == null || OnSetState == null)
			{
				CatLog.Err($"[StateMachine._Ready]：检测挂载对象并非 Player/Npc ，已销毁");
				CatUtils.StopAndExit(this);
				return;
			}

			m_Creature.OnHit += OnHit;

		}

		public override void _PhysicsProcess(double delta)
		{
			MoveState();
		}

		private void OnHit(float damage, Node node) => SwitchAnimState(damage >= m_Creature.m_StaggerDamage ? AnimState.Stagger : m_AnimState);


		private void MoveState()
		{
			if (m_AnimState == AnimState.Death) return;
			if (m_AnimState == AnimState.Stagger) return;
			if (m_AnimState == AnimState.Attack) return;

			if (!IsOnFloor)
			{
				SwitchAnimState(m_Creature.Velocity.Y > 0 ? AnimState.Jump : AnimState.Fall);
			}
			else
			{
				SwitchAnimState(Speed > 0.1f ? AnimState.Walk : AnimState.Idle);
			}
		}


		/// <summary> 切换移动状态，状态不变则不执行 </summary>
		public void SwitchAnimState(AnimState newState)
		{
			if (m_AnimState == newState) return;

			m_AnimState = newState;
			//CatLog.Ok($"[State] Changed to: {newState}");
		}

		public void SwitchPlayerState(PlayerState newState)
		{
			if (m_PlayerState == newState) return;

			m_PlayerState = newState;
			//CatLog.Ok($"[State] Changed to: {newState}");
		}

		public void SwitchNpcState(NpcState newState)
		{
			if (m_NpcState == newState) return;

			m_NpcState = newState;
			//CatLog.Ok($"[State] Changed to: {newState}");
		}

		public void EndAttack()
		{
			if (m_AnimState != AnimState.Attack) return;
			SwitchAnimState(Speed > 0.1f ? AnimState.Walk : AnimState.Idle);
		}

		private void EndStagger()
		{
			if (m_AnimState != AnimState.Stagger) return;

			SwitchAnimState(Speed > 0.1f ? AnimState.Walk : AnimState.Idle);
		}

		private void EndDeath()
		{
			if (m_AnimState != AnimState.Death) return;
			CatUtils.StopAndExit(m_Creature);
		}


		public int GetAnimState() => (int)m_AnimState;

		public int GetState() => OnGetState.Invoke();

		public void SetAnimState(int State) => m_AnimState = (AnimState)State;

		public void SetState(int State) => OnSetState.Invoke(State);
	}
}
