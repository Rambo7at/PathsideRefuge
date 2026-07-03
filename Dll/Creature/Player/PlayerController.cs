using Godot;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Creature.StateMachine;

namespace 途畔归所.Dll.Creature
{
    [GlobalClass]
    public partial class PlayerController : Node
    {
        private Player m_Player;
        private Node3D m_PlayerMesh;
        private Camera3D m_Camera3D;
        private SpringArm3D m_springArm3D;

        // 从 Player 中获取的固定数值
        private float Speed => m_Player.m_Speed;
        private float Jump => m_Player.m_Jump;
        private float targetAngle = Mathf.Pi;

        public override void _Ready()
        {
            if (GetParent() is not Player pl)
            {
                CatLog.Err($"[PlayerController._Ready]：检测挂载对象并非 player ，已销毁");
                CatUtils.StopAndExit(this);
                return;
            }

            if (pl.m_IsOwner == false)
            {
                CatLog.Net($"[PlayerController._Ready]：非所有组件，已销毁");
                CatUtils.StopAndExit(this);
                return;
            }


            m_springArm3D ??= CatUtils.FindChildNode<SpringArm3D>(pl);

            if (m_springArm3D == null)
            {
                CatLog.Warn($"[PlayerController._Ready]：未通找到 m_springArm3D ，已销毁");
                CatUtils.StopAndExit(this);
                return;
            }

            m_Player = pl;
            m_PlayerMesh = pl.m_PlayerModel;
            m_Camera3D = WorldManager.Instance.GetCamera();
        }



        public override void _Process(double delta)
        {
            PlayerMoveAnimationDirection(delta);
        }


        public override void _PhysicsProcess(double delta)
        {
            Attack();
            m_Player.ApplyGravity(delta);
            HandlePlayerMovement(delta);
            m_Player.MoveAndSlide();
        }

        private void HandlePlayerMovement(double delta)
        {
            Vector3 velocity = m_Player.Velocity;

            if (Input.IsActionJustPressed("ui_accept") && m_Player.IsOnFloor() && m_Player.m_AnimState != AnimState.Attack)
            {
                velocity.Y = Jump;
            }

            Vector2 inputDir = Input.GetVector("cat_Left", "cat_Right", "cat_Forward", "cat_Backward");
            Vector3 direction = GetCameraRelativeDirection(inputDir);

            if (m_Player.IsOnFloor() && m_Player.m_AnimState != AnimState.Attack)
            {

                ApplyGroundMovement(direction, ref velocity, 1);

            }
            else if (m_Player.IsOnFloor() && m_Player.m_AnimState == AnimState.Attack)
            {
                ApplyGroundMovement(direction, ref velocity, 0.1f);
            }
            else
            {
                velocity.X *= 0.98f;
                velocity.Z *= 0.98f;
            }

            m_Player.Velocity = velocity;
        }

        private void PlayerMoveAnimationDirection(double delta)
        {
            float cameraAngle = m_Camera3D.GlobalRotation.Y;
            Vector2 inputDir = Input.GetVector("cat_Left", "cat_Right", "cat_Forward", "cat_Backward");

            // ★ 只有在地面上且有输入时，才更新目标朝向
            if (m_Player.IsOnFloor() && inputDir != Vector2.Zero)
            {
                float inputAngle = Mathf.Atan2(inputDir.X, inputDir.Y);
                targetAngle = cameraAngle + inputAngle;
            }

            // 平滑旋转（无论地面还是空中，都会平滑到 targetAngle，空中保持不变）
            float rotationSpeed = 15f;
            float playerTargetY = targetAngle - Mathf.Pi;
            float currentY = m_Player.GlobalRotation.Y;
            float smoothedY = Mathf.LerpAngle(currentY, playerTargetY, (float)delta * rotationSpeed);

            m_Player.GlobalRotation = new Vector3(
                m_Player.GlobalRotation.X,
                smoothedY,
                m_Player.GlobalRotation.Z
            );
        }

        private void Attack()
        {
            m_Player.m_AnimState = Input.IsActionJustPressed("cat_Attack") ? AnimState.Attack : m_Player.m_AnimState;
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


        private void ApplyGroundMovement(Vector3 direction, ref Vector3 velocity, float speedMultiplier)
        {
            if (direction != Vector3.Zero)
            {
                velocity.X = direction.X * Speed * speedMultiplier;
                velocity.Z = direction.Z * Speed * speedMultiplier;
            }
            else
            {
                velocity.X = Mathf.MoveToward(velocity.X, 0, Speed * speedMultiplier);
                velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed * speedMultiplier);
            }
        }
    }
}
