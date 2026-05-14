using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Creature
{
	[GlobalClass]
	public partial class PlayerController : Node
	{
		private Player m_player;
		private Node3D m_PlayerMesh;
		private Camera3D m_Camera3D;

		private PlayerStateMachine m_StateMachine;


		// 从 Player 中获取的固定数值
		private  float Speed;
		private  float JumpVelocity;
		private float targetAngle = Mathf.Pi;

		private Vector3 airMomentum;                  // 起跳时保存的水平速度
		private float airControlFactor = 0.2f;        // 空中可控系数（0～1），越小惯性越强
		private float airDrag = 0.98f;                // 每物理帧水平速度保留比例

		private bool _IsOwner = false;

		public override void _Ready()
		{
			var node = GetParent();
			if (node == null)
			{
				CatLog.Err($"[PlayerController._Ready]：检测挂载对象是空，已返回");
				QueueFree();
				return;
			}

			if (node is not Player pl)
			{
				CatLog.Err($"[PlayerController._Ready]：检测挂载对象并非 player ，已返回");
				QueueFree();
				return;
			}

			var nodeaar = pl.GetChildren();

			foreach (var comp in nodeaar)
			{
				if (comp == null) continue;
				if (comp is PlayerStateMachine StateMachine) m_StateMachine = StateMachine;
				if (comp is NetSyncBase netSyncBase) _IsOwner = netSyncBase.IsOwner;
			}

			if (m_StateMachine == null || _IsOwner == false)
			{
				CatLog.Err($"[PlayerController._Ready]：检测player对象未有状态机组件，已返回");
				QueueFree();
				return;
			}



			m_player = pl;
			m_PlayerMesh = pl.m_PlayerModel;
			m_Camera3D = GameCore.Instance.GetCamera();
			Speed = pl.m_PlayerData.m_Speed;
			JumpVelocity = pl.m_PlayerData.m_Jump;


		}



		public override void _Process(double delta)
		{
			if (_IsOwner == false) return;
			Update(delta);
		}


		public override void _PhysicsProcess(double delta)
		{
			if (_IsOwner == false) return;
			PhysicsUpdate(delta);
		}





		/// <summary>
		/// 每帧逻辑更新（输入、旋转）
		/// </summary>
		public void Update(double delta)
		{

			PlayerMoveAnimationDirection(delta);
		}

		/// <summary>
		/// 物理更新（移动、重力、跳跃）
		/// </summary>
		public void PhysicsUpdate(double delta)
		{
			m_player.ApplyGravity(delta);
			HandlePlayerMovement(delta);

		}

		private void HandlePlayerMovement(double delta)
		{

			if (m_StateMachine.s_PlayerState == PlayerStateMachine.PlayerState.Attack) return;
			Vector3 velocity = m_player.Velocity;

			// 跳跃
			if (Input.IsActionJustPressed("ui_accept") && m_player.IsOnFloor())
			{
				velocity.Y = JumpVelocity;
				// 记录起跳瞬间的水平速度（包括因移动带来的速度）
				airMomentum = new Vector3(velocity.X, 0, velocity.Z);
			}

			// 获取输入方向
			Vector2 inputDir = Input.GetVector("cat_Left", "cat_Right", "cat_Forward", "cat_Backward");

			// 计算摄像机方向
			Vector3 cameraForward = -m_Camera3D.GlobalTransform.Basis.Z;
			Vector3 cameraRight = m_Camera3D.GlobalTransform.Basis.X;
			cameraForward.Y = 0;
			cameraRight.Y = 0;
			cameraForward = cameraForward.Normalized();
			cameraRight = cameraRight.Normalized();

			Vector3 direction = cameraForward * (-inputDir.Y) + cameraRight * inputDir.X;
			direction = direction.Normalized();

			if (m_player.IsOnFloor())
			{
				// 地面移动逻辑（保持原样）
				if (direction != Vector3.Zero)
				{
					velocity.X = direction.X * Speed;
					velocity.Z = direction.Z * Speed;
				}
				else
				{
					velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
					velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed);
				}
			}
			else
			{
				// 空中移动逻辑（惯性为主，输入为辅）
				// 1. 基础摩擦力
				velocity.X *= airDrag;
				velocity.Z *= airDrag;

				// 2. 如果玩家有输入，则在惯性基础上叠加微调
				if (direction != Vector3.Zero)
				{
					// 目标速度 = 输入方向 * 当前速度大小（保留惯性大小感）或固定 Speed
					float targetSpeed = Mathf.Max(airMomentum.Length(), Speed * 0.5f);
					Vector3 targetVelocity = direction * targetSpeed;

					// 将当前速度向目标速度平滑插值，airControlFactor 控制空中转向力度
					velocity.X = Mathf.Lerp(velocity.X, targetVelocity.X, airControlFactor);
					velocity.Z = Mathf.Lerp(velocity.Z, targetVelocity.Z, airControlFactor);
				}
				// 无输入时，速度自然衰减（airDrag 已处理）
			}

			m_player.Velocity = velocity;
			m_player.MoveAndSlide();
		}

		private void PlayerMoveAnimationDirection(double delta)
		{
			
			float cameraAngle = m_Camera3D.GlobalRotation.Y;

			Vector2 inputDir = Input.GetVector("cat_Left", "cat_Right", "cat_Forward", "cat_Backward");

			float inputAngle = Mathf.Atan2(inputDir.X, inputDir.Y);

			if (inputDir != Vector2.Zero)
			{
				targetAngle = cameraAngle + inputAngle;
			}

			float rotationSpeed = 15f;
			float smoothedY = Mathf.LerpAngle(m_PlayerMesh.GlobalRotation.Y, targetAngle, (float)delta * rotationSpeed);

			m_PlayerMesh.GlobalRotation = new Vector3(
				m_PlayerMesh.GlobalRotation.X,
				smoothedY,
				m_PlayerMesh.GlobalRotation.Z
			);
		}

	}
}
