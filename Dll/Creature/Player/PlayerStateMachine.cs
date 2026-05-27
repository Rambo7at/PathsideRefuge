using Godot;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Creature
{
    [GlobalClass]
    public partial class PlayerStateMachine : Node
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
            AttackFinished = 8
        }

		private Player m_player;

		private PlayerState m_CurrentState = PlayerState.Idle;

		public bool Walk => m_CurrentState == PlayerState.Walk;

		public bool Jump => m_CurrentState == PlayerState.Jump;

		public bool Idle => m_CurrentState == PlayerState.Idle;

		public bool Attack => m_CurrentState == PlayerState.Attack;

        public bool AttackFinished => m_CurrentState == PlayerState.AttackFinished;


        public PlayerState s_PlayerState => m_CurrentState;

		public override void _Ready()
		{
            var node = GetParent();

			if (node == null)
			{
				CatLog.Err($"[PlayerStateMachine._Ready]：检测挂载对象是空，已返回");
				CatUtils.StopAndExit(this);
				return;
			}

            if (node is not Player pl)
            {
                CatLog.Err($"[PlayerController._Ready]：检测挂载对象并非 player ，已返回");
                CatUtils.StopAndExit(this);
                return;
            }

            m_player = pl;

            if (m_player.m_PlayerData == null)
			{
                SetProcess(false);
                SetPhysicsProcess(false);
                return;
            }
		}

		public override void _PhysicsProcess(double delta)
		{
            if (m_player.m_PlayerData == null) return;


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

			m_CurrentState = newState;
			//CatLog.Ok($"[State] Changed to: {newState}");
		}



		/// <summary> 注：根据玩家速度和是否在地面自动切换物理状态 </summary>
		private void UpdatePhysicsBasedState()
		{
			if (m_CurrentState == PlayerState.Attack) return;

            if (Input.IsActionJustPressed("cat_Attack"))
            {
                SwitchState(PlayerState.Attack);
                return; // 攻击触发后不再判断 Walk/Idle
            }


            // 地面状态：优先检测攻击输入
            if (m_player.IsOnFloor())
			{

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



        /// <summary> 注：给动画调用的函数 </summary>
        public void EndAttack()
        {
            if (m_CurrentState != PlayerState.Attack) return;

            // 根据当前水平速度决定下一个状态
            Vector3 horizontalVel = new(m_player.Velocity.X, 0, m_player.Velocity.Z);
            PlayerState nextState = horizontalVel.Length() > 0.1f ? PlayerState.Walk : PlayerState.Idle;

            SwitchState(nextState);
            //CatLog.Ok("执行了EndAttack函数 -> 切换到 " + nextState);
        }


    }
}
