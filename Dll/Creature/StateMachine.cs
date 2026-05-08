using Godot;

namespace 途畔归所.Dll.Creature
{
	public partial class StateMachine : Node3D
	{
		public enum PlayerState
		{
			Idle = 0,
			Walk = 1,
			Run = 2,
			Jump = 3,
			Fall = 4,
			Interact = 5, // 开箱子/UI 锁定
			Build = 6,     // 建造预览模式
			Attack = 7,
		}

		[Export] private Player m_player;

		private PlayerState m_CurrentState = PlayerState.Idle;

		public bool Walk => m_CurrentState == PlayerState.Walk;

		public bool Jump => m_CurrentState == PlayerState.Jump;

		public bool Idle => m_CurrentState == PlayerState.Idle;

		public bool Attack => m_CurrentState == PlayerState.Attack;
		public PlayerState s_PlayerState => m_CurrentState;

		public override void _Ready()
		{
			if (m_player == null)
			{
				GD.PrintErr("[StateMachine]：m_player 字段为空");
				return;
			}
		}

		public override void _PhysicsProcess(double delta)
		{
			// 只有在非锁定状态下才自动检测物理状态切换
			if (m_CurrentState != PlayerState.Interact && m_CurrentState != PlayerState.Build)
			{
				UpdatePhysicsBasedState();
			}
		}

		/// <summary> 注：切换玩家状态，状态不变则不执行 </summary>
		public void SwitchState(PlayerState newState)
		{
			if (m_CurrentState == newState) return;

			// 这里以后可以加：OnExitState(m_CurrentState);
			m_CurrentState = newState;
			// 这里以后可以加：OnEnterState(newState);

			//GD.Print($"[State] Changed to: {newState}");
		}

		/// <summary> 注：根据玩家速度和是否在地面自动切换物理状态 </summary>
		private void UpdatePhysicsBasedState()
		{
			if (m_CurrentState == PlayerState.Attack) return;
			// 地面状态：优先检测攻击输入
			if (m_player.IsOnFloor())
			{
				if (Input.IsActionJustPressed("cat_Attack"))
				{
					SwitchState(PlayerState.Attack);
					return; // 攻击触发后不再判断 Walk/Idle
				}


				Vector3 horizontalVel = new(m_player.Velocity.X, 0, m_player.Velocity.Z);
				float speed = horizontalVel.Length();

				if (speed > 0.1f)
				{
					SwitchState(PlayerState.Walk);
				}
				else
				{
					SwitchState(PlayerState.Idle);
				}
			}
			else
			{
				// 在空中
				SwitchState(m_player.Velocity.Y > 0 ? PlayerState.Jump : PlayerState.Fall);
			}
		}


		public void EndAttack()
		{
			if (m_CurrentState == PlayerState.Attack) SwitchState(PlayerState.Idle);

		}
	}
}
