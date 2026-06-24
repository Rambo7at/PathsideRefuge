using Godot;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Creature
{
    [GlobalClass]
    public partial class CreatureAttackComp : Node
    {
        [Export] private Area3D m_emptyhanded;

        public Area3D m_CustomHitBox => (m_Humanoid != null && m_Humanoid.m_EquipHitBox != null) ? m_Humanoid.m_EquipHitBox : null;

        private CreatureBase m_CreatureBase;
        private Humanoid m_Humanoid;

        private Area3D m_hitBox => m_CustomHitBox == null ? m_emptyhanded : m_CustomHitBox;

        public override void _Ready()
        {
            if (GetParent() is not CreatureBase creature)
            {
                CatLog.Err("[CreatureAttack._Ready] 挂载节点不是 CreatureBase，已销毁");
                CatUtils.StopAndExit(this);
                return;
            }
            m_CreatureBase = creature;
            m_Humanoid = creature is Humanoid human ? human : null;
        }

        // 动画轨道调用：开启判定窗口
        public void EnableHitbox()
        {
            if (m_hitBox == null) return;
            m_hitBox.Monitoring = true;
            m_hitBox.BodyEntered += OnHit;
        }

        // 动画轨道调用：关闭判定窗口
        public void DisableHitbox()
        {
            if (m_hitBox == null) return;
            m_hitBox.BodyEntered -= OnHit;
            m_hitBox.Monitoring = false;
        }

        // Area3D回调函数：在这个方法里做伤害逻辑
        private void OnHit(Node3D body)
        {
            if (body == m_CreatureBase || body is not IDamageable node) return;

            node.TakeDamage(m_CreatureBase.m_Damage);

            CatLog.Ok($"[PlayerAttack] 命中 {body.Name}");
        }
    }
}
