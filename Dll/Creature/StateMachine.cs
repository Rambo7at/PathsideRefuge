using Godot;
using System;
using System.Xml.Linq;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Creature.Npc;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.NetWork;
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
			Menu = 3
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

		// 状态属性
		public AnimState m_AnimState { get; private set; } = AnimState.Idle;
		public NpcState m_NpcState { get; private set; } = NpcState.Patrol;
		public PlayerState m_PlayerState { get; private set; } = PlayerState.Idle;

		// 动画表达式
		public bool Walk => m_AnimState == AnimState.Walk;
		public bool Jump => m_AnimState == AnimState.Jump;
		public bool Idle => m_AnimState == AnimState.Idle;
		public bool Attack => m_AnimState == AnimState.Attack;
		public bool Stagger => m_AnimState == AnimState.Stagger;
		public bool Death => m_AnimState == AnimState.Death;
		public int AttackAnimIndex { get; private set; }
		public bool IsCombo { get; set; }
		public bool GoCombo { get; set; }


		// 便捷属性
		private bool IsOnFloor => m_Creature.IsOnFloor();
		private float Speed => new Vector3(m_Creature.Velocity.X, 0, m_Creature.Velocity.Z).Length();


		// 私有字段
		private CreatureBase m_Creature;
		private Humanoid m_Humanoid => m_Creature is Humanoid human ? human : null;
		private Player m_Player => m_Humanoid is Player pl ? pl : null;
		private Creature.Npc.Npc m_Npc => m_Humanoid is Creature.Npc.Npc npc ? npc : null;


		// RPC委托
		private Func<int> OnGetState;
		private Action<int> OnSetState;

		// 接口事件
		public event Action OnComboRequested;
		public event Action OnAnimStateChanged;
		public event Action<int> OnAttackAnimIndexChanged;
		public event Action OnOneShotChanged;

		public override void _Ready()
		{
			if (GetParent() is not CreatureBase cr)
			{
				CatUtils.StopAndExit(this, $"[StateMachine._Ready]：检测挂载对象并非 CreatureBase ，已销毁");
				return;
			}

			m_Creature = cr;

			InitPlayerStateMachine();
			InitNpcStateMachine();

			m_Creature.OnHit += OnHit;
			m_Creature.m_AnimComp.OnEndStagger += EndStagger;
			m_Creature.m_AnimComp.OnEndDeath += EndDeath;
			m_Creature.m_AnimComp.OnEndAttack += EndAttack;
			m_Creature.m_AnimComp.OnEndCombo += EndCombo;
			m_Creature.m_AnimComp.OnCombo += Combo;

			
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


		/// <summary> 注：切换动画状态，状态不变则不执行 </summary>
		public void SwitchAnimState(AnimState newState)
		{
			if (m_AnimState == newState) return;
			m_AnimState = newState;
			OnAnimStateChanged?.Invoke();
			//CatLog.Ok($"[State] Changed to: {newState}");
		}

		/// <summary> 注：切换玩家状态，状态不变则不执行 </summary>
		public void SwitchPlayerState(PlayerState newState)
		{
			if (m_PlayerState == newState) return;

			m_PlayerState = newState;
			//CatLog.Ok($"[State] Changed to: {newState}");
		}

		/// <summary> 注：切换NPC状态，状态不变则不执行 </summary>
		public void SwitchNpcState(NpcState newState)
		{
			if (m_NpcState == newState) return;

			m_NpcState = newState;
			//CatLog.Ok($"[State] Changed to: {newState}");
		}

		/// <summary> 注：切换切换攻击动作索引 </summary>
		public void SwitchAttackAnimIndex(int index) => AttackAnimIndex = index;

		public void RequestAttack()
		{
			SwitchAnimState(AnimState.Attack);
			OnOneShotChanged?.Invoke();
		}

		public void RequestCombo() => TriggerCombo();

		/// <summary>注：初始化连段检测</summary>
		public void Combo()
		{
			IsCombo = false;
			GoCombo = false;
		}

		/// <summary>注：让动画使用表达式，跳转衍生连段</summary>
		public void EndCombo()
		{
			CatLog.Ok($"[EndCombo] 执行 EndAttack {IsCombo} {GoCombo}");
			if (IsCombo)
			{
				GoCombo = true;
			}
		}

		/// <summary>注：结束攻击</summary>
		public void EndAttack()
		{

			if (m_AnimState != AnimState.Attack) return;
			IsCombo = false;
			GoCombo = false;
			SwitchAnimState(Speed > 0.1f ? AnimState.Walk : AnimState.Idle);
		}
		/// <summary>注：结束眩晕</summary>
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

		public void TriggerOneShot() => m_Creature.m_AnimationTree.Set("parameters/OneShot/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);

        public void TriggerCombo() => OnComboRequested?.Invoke();

        public void TriggerAttackAnimIndex(int index) => OnAttackAnimIndexChanged.Invoke(index);

        private void InitPlayerStateMachine()
		{
			if (m_Player == null) return;

			OnGetState += () => (int)m_PlayerState;
			OnSetState += (index) => m_PlayerState = (PlayerState)index;
            OnAttackAnimIndexChanged += (index) => AttackAnimIndex = index;
            OnOneShotChanged += TriggerOneShot;
			OnComboRequested += () => IsCombo = true;

			if (m_Player != null && !m_Player.m_IsOwner)
			{
				SetPhysicsProcess(false);
			}
		}

		private void InitNpcStateMachine()
		{
			if (m_Npc == null) return;
			OnGetState += () => (int)m_NpcState;
			OnSetState += (index) => m_NpcState = (NpcState)index;
            OnAttackAnimIndexChanged += (index) => AttackAnimIndex = index;

            if (m_Npc != null && !m_Npc.m_IsOwner)
			{
				SetPhysicsProcess(false);
			}
		}

    }
}
