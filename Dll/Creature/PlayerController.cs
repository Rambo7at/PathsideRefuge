using Godot;

namespace 途畔归所.Dll.Creature
{
    public class PlayerController
    {
        private Player player;

        private Node3D m_PlayerMesh;
        private Camera3D m_Camera3D;

        // 从 Player 中获取的固定数值
        private  float Speed;
        private  float JumpVelocity;
        private float targetAngle = Mathf.Pi;

        public PlayerController(Player pl)
        {
            player = pl;
            m_PlayerMesh = player.玩家模型;
            m_Camera3D = player.摄像机;
            Speed = player.m_Speed;
            JumpVelocity = player.m_Jump;
        }

        /// <summary>
        /// 每帧逻辑更新（输入、旋转）
        /// </summary>
        public void Update(double delta) => PlayerMoveAnimationDirection(delta);

        /// <summary>
        /// 物理更新（移动、重力、跳跃）
        /// </summary>
        public void PhysicsUpdate(double delta) => HandlePlayerMovement(delta);

        private void HandlePlayerMovement(double delta)
        {
            Vector3 velocity = player.Velocity;

            // 重力
            if (!player.IsOnFloor())
            {
                velocity += player.GetGravity() * (float)delta;
            }

            // 跳跃
            if (Input.IsActionJustPressed("ui_accept") && player.IsOnFloor())
            {
                velocity.Y = JumpVelocity;
            }

            // 获取输入方向
            Vector2 inputDir = Input.GetVector("cat_Left", "cat_Right", "cat_Forward", "cat_Backward");

            // 计算摄像头方向
            Vector3 cameraForward = -m_Camera3D.GlobalTransform.Basis.Z;
            Vector3 cameraRight = m_Camera3D.GlobalTransform.Basis.X;
            cameraForward.Y = 0;
            cameraRight.Y = 0;
            cameraForward.Normalized();
            cameraRight.Normalized();

            // 计算移动方向
            Vector3 direction = cameraForward * (-inputDir.Y) + cameraRight * inputDir.X;
            direction.Normalized();

            // 应用速度
            if (direction != Vector3.Zero)
            {
                velocity.X = direction.X * Speed;
                velocity.Z = direction.Z * Speed;
            }
            else
            {
                velocity.X = Mathf.MoveToward(player.Velocity.X, 0, Speed);
                velocity.Z = Mathf.MoveToward(player.Velocity.Z, 0, Speed);
            }

            player.Velocity = velocity;
            player.MoveAndSlide();
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