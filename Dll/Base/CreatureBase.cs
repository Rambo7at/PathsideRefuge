using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 途畔归所.Dll.Base
{
    public partial class CreatureBase : CharacterBody3D
    {

        [Export] public float m_Health = 100f;

        [Export] public AnimationTree m_AnimationTree;

        /// <summary> 射线组件 </summary>
        [Export] public Marker3D m_eye;

        public bool IsDead = false;

        public bool IsMoving { get => new Vector3(Velocity.X, 0, Velocity.Z).Length() > 0f; }
        public bool IsJump { get => !IsOnFloor(); }

        public override void _Ready()
        {
         
        }


        /// <summary>
        /// 受伤逻辑（虚函数，子类可重写）
        /// </summary>
        public virtual void UnderDamage(float damage, Node3D attacker = null)
        {
            if (IsDead) return;

            m_Health = Mathf.Max(m_Health - damage, 0);

            if (m_Health <= 0) Die();

        }



        /// <summary>
        /// 死亡逻辑（虚函数）
        /// </summary>
        public virtual void Die()
        {
            IsDead = true;
        }


        /// <summary>
        /// 判断当前是否为网络权威（主机/本地玩家）
        /// 对应 Valheim : IsOwner()
        /// </summary>
        public bool IsNetworkAuthority()
        {
            return IsMultiplayerAuthority();
        }


        public virtual void UpdateGravity(double delta)
        {
            if (IsOnFloor()) return;
            Velocity += GetGravity() * (float)delta;
            MoveAndSlide();
        }



    }
}
