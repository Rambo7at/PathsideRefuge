using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Data;

namespace 途畔归所.Dll.Base
{
    public partial class CreatureBase : CharacterBody3D
    {
        [Export] public float m_Health = 100f;
        [Export] public AnimationTree m_AnimationTree;
        [Export] public RayCast3D m_eye;

        private float targetAngle;
        public bool IsDead = false;

        public bool IsMoving { get => new Vector3(Velocity.X, 0, Velocity.Z).Length() > 0f; }
        public bool IsJump { get => !IsOnFloor(); }

        public override void _Ready()
        {

        }

        /// <summary> 注：受伤逻辑（虚函数，子类可重写） </summary>
        public virtual void UnderDamage(float damage, Node3D attacker = null)
        {
            if (IsDead) return;

            m_Health = Mathf.Max(m_Health - damage, 0);

            if (m_Health <= 0) Die();
        }

        /// <summary> 注：死亡逻辑（虚函数） </summary>
        public virtual void Die()
        {
            IsDead = true;
        }

        /// <summary> 注：判断当前是否为网络权威（主机/本地玩家）对应 Valheim : IsOwner() </summary>
        public bool IsNetworkAuthority()
        {
            return IsMultiplayerAuthority();
        }

        /// <summary> 注：重力 </summary>
        public virtual void ApplyGravity(double delta)
        {
            if (IsOnFloor()) return;
            Velocity += GetGravity() * (float)delta;
        }

        /// <summary> 注：水平移动 </summary>
        public virtual void MoveHorizontally(Vector3 direction, float speed)
        {
            Vector3 vel = Velocity;
            if (direction != Vector3.Zero)
            {
                vel.X = direction.X * speed;
                vel.Z = direction.Z * speed;
            }
            else
            {
                vel.X = 0;
                vel.Z = 0;
            }
            Velocity = vel;
        }

        /// <summary> 注：智能转向，移动时面朝速度方向，静止时面朝目标点 </summary>
        protected void FaceMovementOrTarget(Vector3 lookTarget, float rotationSpeed, float delta)
        {
            Vector3 horizontalVel = new Vector3(Velocity.X, 0, Velocity.Z);
            Vector3 target;

            if (horizontalVel.LengthSquared() > 0.01f)
            {
                target = GlobalPosition + horizontalVel;
                // 移动中：朝速度方向
            }
            else
            {
                target = lookTarget; // 静止：朝最终目标
            }
                               

            Vector3 dir = target - GlobalPosition;
            dir.Y = 0;
            if (dir.LengthSquared() < 0.001f) return;

            float targetAngle = Mathf.Atan2(dir.X, dir.Z) - Mathf.Pi;
            float newY = Mathf.LerpAngle(GlobalRotation.Y, targetAngle, rotationSpeed * delta);
            GlobalRotation = new Vector3(GlobalRotation.X, newY, GlobalRotation.Z);
        }


    }
}