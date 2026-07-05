using Godot;
using System;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Creature
{
    public partial class Attack : Node
    {

        private CreatureBase m_CreatureBase;
        private Area3D m_HitBox;




        public override void _Ready()
        {
            // 自检
            if (GetParent() is not CreatureBase creature)
            {
                CatLog.Err("[CreatureAttack._Ready] 挂载节点不是 CreatureBase，已销毁");
                CatUtils.StopAndExit(this);
                return;
            }
            m_CreatureBase = creature;

            m_CreatureBase.m_AnimComp.OnEnableHitbox = EnableHitbox;
            m_CreatureBase.m_AnimComp.OnEndDeath = DisableHitbox;
        }

        // 动画轨道调用：开启判定窗口
        public void EnableHitbox()
        {
            if (m_HitBox == null) return;
            m_HitBox.Monitoring = true;
            m_HitBox.BodyEntered += OnHit;
        }

        // 动画轨道调用：关闭判定窗口
        public void DisableHitbox()
        {
            if (m_HitBox == null) return;
            m_HitBox.BodyEntered -= OnHit;
            m_HitBox.Monitoring = false;
        }

        /// <summary>注 Area3D回调函数 </summary>
        private void OnHit(Node3D body)
        {
            if (body == m_CreatureBase || body is not IDamageable node) return;

            node.TakeDamage(m_CreatureBase.m_Damage);

            CatLog.Ok($"[PlayerAttack] 命中 {body.Name}");
        }
    }
}
