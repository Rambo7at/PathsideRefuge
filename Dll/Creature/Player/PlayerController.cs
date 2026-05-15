using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Creature.PlayerStateMachine;

namespace 途畔归所.Dll.Creature
{
	[GlobalClass]
	public partial class PlayerController : Node
	{
		private Player m_player;
		private Node3D m_PlayerMesh;
		private Camera3D m_Camera3D;
		private SpringArm3D m_springArm3D;



        private PlayerStateMachine m_StateMachine;


		// 从 Player 中获取的固定数值
		private  float Speed;
		private  float JumpVelocity;
		private float targetAngle = Mathf.Pi;

		private bool _IsOwner = false;

		public override void _Ready()
		{
			var node = GetParent();

			if (node is not Player pl)
			{
				CatLog.Err($"[PlayerController._Ready]：检测挂载对象并非 player ，已销毁");
				CatUtils.StopAndExit(this);
				return;
			}

			var nodeaar = pl.GetChildren();

			foreach (var comp in nodeaar)
			{
				if (comp is PlayerStateMachine StateMachine) m_StateMachine = StateMachine;
				if (comp is NetSyncBase netSyncBase) _IsOwner = netSyncBase.IsOwner;
				if (comp is SpringArm3D springArm3D) m_springArm3D = springArm3D;
            }

			if (m_StateMachine == null || _IsOwner == false || m_StateMachine == null)
			{
				CatLog.Err($"[PlayerController._Ready]：检测部分未通过，已销毁");
                CatUtils.StopAndExit(this);
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
            PlayerMoveAnimationDirection(delta);
        }


		public override void _PhysicsProcess(double delta)
		{
            m_player.ApplyGravity(delta);
            HandlePlayerMovement(delta);
        }

		private void HandlePlayerMovement(double delta)
		{
			Vector3 velocity = m_player.Velocity;

			if (Input.IsActionJustPressed("ui_accept") && m_player.IsOnFloor() && m_StateMachine.s_PlayerState != PlayerState.Attack)
			{
                velocity.Y = JumpVelocity;
            }

            Vector2 inputDir = Input.GetVector("cat_Left", "cat_Right", "cat_Forward", "cat_Backward"); 
            Vector3 direction = GetCameraRelativeDirection(inputDir);

            if (m_player.IsOnFloor() && m_StateMachine.s_PlayerState != PlayerState.Attack)
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
			else if ((m_player.IsOnFloor() && m_StateMachine.s_PlayerState == PlayerState.Attack))
			{

                // 地面移动逻辑（保持原样）
                if (direction != Vector3.Zero)
                {
                    velocity.X = direction.X * (Speed * 0.1f);
                    velocity.Z = direction.Z * (Speed * 0.1f);
                }
                else
                {
                    velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
                    velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed);
                }

            }
			else
			{
				velocity.X *= 0.98f;
				velocity.Z *= 0.98f;
			}

			m_player.Velocity = velocity;
			m_player.MoveAndSlide();
		}

        private void PlayerMoveAnimationDirection(double delta)
        {
            float cameraAngle = m_Camera3D.GlobalRotation.Y;
            Vector2 inputDir = Input.GetVector("cat_Left", "cat_Right", "cat_Forward", "cat_Backward");

            // ★ 只有在地面上且有输入时，才更新目标朝向
            if (m_player.IsOnFloor() && inputDir != Vector2.Zero)
            {
                float inputAngle = Mathf.Atan2(inputDir.X, inputDir.Y);
                targetAngle = cameraAngle + inputAngle;
            }

            // 平滑旋转（无论地面还是空中，都会平滑到 targetAngle，空中保持不变）
            float rotationSpeed = 15f;
            float playerTargetY = targetAngle - Mathf.Pi;
            float currentY = m_player.GlobalRotation.Y;
            float smoothedY = Mathf.LerpAngle(currentY, playerTargetY, (float)delta * rotationSpeed);

            m_player.GlobalRotation = new Vector3(
                m_player.GlobalRotation.X,
                smoothedY,
                m_player.GlobalRotation.Z
            );
        }




        /// <summary>
        /// 根据摄像机方向，将玩家输入（WASD）转换为世界移动方向（水平）。
        /// </summary>
        private Vector3 GetCameraRelativeDirection(Vector2 inputDir)
        {
            Vector3 forward = -m_Camera3D.GlobalTransform.Basis.Z;
            Vector3 right = m_Camera3D.GlobalTransform.Basis.X;

            forward.Y = 0;
            right.Y = 0;
            forward = forward.Normalized();
            right = right.Normalized();

            // 注意：-inputDir.Y 是为了匹配你当前的输入映射
            Vector3 direction = forward * (-inputDir.Y) + right * inputDir.X;
            return direction.LengthSquared() > 0.001f ? direction.Normalized() : Vector3.Zero;
        }



    }
}
