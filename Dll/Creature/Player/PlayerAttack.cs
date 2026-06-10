using Godot;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Creature
{
    [GlobalClass]
    public partial class PlayerAttack : Node
    {

        private Player m_player;
        private Area3D m_hitBox;

        public override void _Ready()
        {
            if (GetParent() is not Player pl)
            {
                CatLog.Err("[PlayerAttack._Ready] 挂载节点不是 Player，已销毁");
                CatUtils.StopAndExit(this);
                return;
            }

            if (pl.m_IsOwner == false)
            {
                CatLog.Net($"[PlayerAttack._Ready]：非所有组件，已销毁");
                CatUtils.StopAndExit(this);
                return;
            }

            m_player = pl;
            m_hitBox = m_player.m_hitBox;
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

        // 判定回调：在这个方法里做伤害逻辑
        private void OnHit(Node3D body)
        {
            if (body == m_player) return;

            if (body is IDamageable node)
            {
                node.TakeDamage(m_player.m_BaseAttackDamage);

                CatLog.Ok($"[PlayerAttack] 命中 {body.Name}");
            }
        }


    }
}