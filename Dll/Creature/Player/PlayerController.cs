using Godot;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Creature.StateMachine;

namespace 途畔归所.Dll.Creature
{
    [GlobalClass]
    public partial class PlayerController : Node
    {
        // 组件
        private Player m_Player;
        private Camera3D m_Camera3D;
        private SpringArm3D m_springArm3D;
        private StateMachine m_StateMachine;

        // 便捷属性
        private AnimState AnimState => m_StateMachine.m_AnimState;
        private PlayerState PlayerState => m_StateMachine.m_PlayerState;
        private bool IsAttackState => AnimState == AnimState.Attack;
        private bool IsMenuState => PlayerState == PlayerState.Menu;


        // 常用值
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
            m_Player = pl;
            m_Camera3D = WorldManager.Instance.GetCamera();
            m_StateMachine = pl.m_StateMachine;

            if (m_springArm3D == null)
            {
                CatLog.Warn($"[PlayerController._Ready]：未通找到 m_springArm3D ，已销毁");
                CatUtils.StopAndExit(this);
                return;
            }

        }



        public override void _Process(double delta)
        {
            if (IsMenuState) return;
            UpdateRotation(delta);
            TryAttack();
        }


        public override void _PhysicsProcess(double delta)
        {

            m_Player.ApplyGravity(delta);
            UpdateMovement(delta);
            m_Player.MoveAndSlide();
        }


        /// <summary>注：移动逻辑总控，整合跳跃与多状态移动速度计算</summary>
        private void UpdateMovement(double delta)
        {
            Vector3 velocity = m_Player.Velocity;

            // Menu状态下阻断移动输入，速度清零
            if (IsMenuState)
            {
                velocity.X = 0;
                velocity.Z = 0;
                m_Player.Velocity = velocity;
                return;
            }

            ApplyJump(ref velocity);
            ApplyMovement(ref velocity);

            m_Player.Velocity = velocity;
        }


        /// <summary>注：处理跳跃输入，仅地面非攻击状态下生效</summary>
        private void ApplyJump(ref Vector3 vector)
        {
            if (IsAttackState) return;

            if (Input.IsActionJustPressed("ui_accept") && m_Player.IsOnFloor())
            {
                vector.Y = Jump;
            }
        }


        /// <summary>注：根据状态应用移动速度，区分正常地面、攻击地面、空中三种情况</summary>
        private void ApplyMovement(ref Vector3 velocity)
        {
            var direction = GetCameraRelativeDirection(Input.GetVector("cat_Left", "cat_Right", "cat_Forward", "cat_Backward"));
            if (m_Player.IsOnFloor() && !IsAttackState)
            {
                // 正常地面移动
                ApplyGroundMovement(direction, ref velocity, 1f);
            }
            else if (m_Player.IsOnFloor() && IsAttackState)
            {
                // 攻击状态地面移动减速
                ApplyGroundMovement(direction, ref velocity, 0.1f);
            }
            else
            {
                // 空中水平速度阻尼衰减
                velocity.X *= 0.98f;
                velocity.Z *= 0.98f;
            }
        }


        /// <summary>注：根据摄像机方向，将二维输入转换为世界空间水平移动方向</summary>
        private Vector3 GetCameraRelativeDirection(Vector2 inputDir)
        {
            Vector3 forward = -m_Camera3D.GlobalTransform.Basis.Z;
            Vector3 right = m_Camera3D.GlobalTransform.Basis.X;

            forward.Y = 0;
            right.Y = 0;
            forward = forward.Normalized();
            right = right.Normalized();

            // -inputDir.Y 适配当前输入映射的前后方向
            Vector3 direction = forward * (-inputDir.Y) + right * inputDir.X;
            return direction.LengthSquared() > 0.001f ? direction.Normalized() : Vector3.Zero;
        }


        /// <summary>辅助：应用地面移动速度，支持倍率控制，无输入时平滑减速至0</summary>
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


        /// <summary>注：攻击输入检测，切换动画状态并触发攻击OneShot动画</summary>
        private void TryAttack()
        {
            if (!Input.IsActionJustPressed("cat_Attack")) return;
            if (IsAttackState)
            {
                m_StateMachine.RequestCombo();
                return;
            }
            // 切换至攻击状态
            m_StateMachine.RequestAttack();
        }


        /// <summary>注：平滑更新玩家朝向，地面随输入更新目标角度，空中保持朝向</summary>
        private void UpdateRotation(double delta)
        {
            float cameraAngle = m_Camera3D.GlobalRotation.Y;
            Vector2 inputDir = Input.GetVector("cat_Left", "cat_Right", "cat_Forward", "cat_Backward");

            // 仅地面有输入时更新目标朝向，空中维持当前朝向
            if (m_Player.IsOnFloor() && inputDir != Vector2.Zero)
            {
                float inputAngle = Mathf.Atan2(inputDir.X, inputDir.Y);
                targetAngle = cameraAngle + inputAngle;
            }

            // 全状态平滑插值旋转
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
    }
}
