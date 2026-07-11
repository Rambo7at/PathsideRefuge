using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Creature.Npc;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Creature.StateMachine;

[GlobalClass]
public partial class NpcMovement : Node3D
{
    [Export] public NavigationAgent3D m_navAgent;

    private Npc m_Npc;
    private StateMachine m_StateMachine;


    // 便捷属性
    private AnimState AnimState => m_StateMachine.m_AnimState;
    private NpcState NpcState => m_StateMachine.m_NpcState;
    private bool IsStaggerState => AnimState == AnimState.Stagger;
    private bool IsDeathState => AnimState == AnimState.Death;

    private Vector3 _safeVelocity = Vector3.Zero;  // 存储 avoidance 后的安全速度

    public override void _Ready()
    {
        if (NetCore.Instance.IsClient)
        {
            CatUtils.StopAndExit(this);
            return;
        }

        if (m_navAgent == null)
        {
            CatLog.Err("[NpcMovement._Ready]：缺少 NavigationAgent3D 组件，已销毁");
            CatUtils.StopAndExit(this);
            return;
        }

        if (GetParent() is not Npc node)
        {
            CatLog.Err("[NpcMovement._Ready]：挂载的对象不是 Npc");
            CatUtils.StopAndExit(this);
            return;
        }

        m_Npc = node;
        m_StateMachine = node.m_StateMachine;
        // 连接 avoidance 计算结果信号
        m_navAgent.VelocityComputed += OnVelocityComputed;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        m_Npc.ApplyGravity(dt);
        ApplyMovement(dt);
        m_Npc.MoveAndSlide();
    }

    private void ApplyMovement(float delta)
    {
        if (m_navAgent.IsNavigationFinished())
        {
            // 停止
            m_navAgent.Velocity = Vector3.Zero;
            m_Npc.Velocity = new Vector3(0, m_Npc.Velocity.Y, 0);
            return;
        }

        if (m_Npc.IsDead || IsStaggerState || IsDeathState)
        {
            m_Npc.Velocity = new Vector3(0, m_Npc.Velocity.Y, 0);
            return;
        }

        // 1. 获取下一个路径点，计算期望水平速度
        Vector3 nextPoint = m_navAgent.GetNextPathPosition();
        Vector3 toTarget = nextPoint - m_Npc.GlobalPosition;
        toTarget.Y = 0;

        Vector3 desiredVelocity = toTarget.Length() > 0.1f? toTarget.Normalized() * m_Npc.m_Speed : Vector3.Zero;

        // 2. 将期望速度提交给导航代理（触发 avoidance 计算）
        m_navAgent.Velocity = desiredVelocity;

        // 3. 使用上一帧计算出的安全速度（由信号更新）
        m_Npc.Velocity = new Vector3(_safeVelocity.X, m_Npc.Velocity.Y, _safeVelocity.Z);

        // 4. 面向移动方向
        if (_safeVelocity.LengthSquared() > 0.01f)
        {
            m_Npc.FaceMovementOrTarget(_safeVelocity, m_Npc.m_RotationSpeed, delta);
        }
    }

    private void OnVelocityComputed(Vector3 safeVelocity)
    {
        _safeVelocity = safeVelocity;
    }

    public void SetNavigation(Vector3 target)
    {
        m_navAgent.TargetPosition = target;
    }

    public void ClearNavigation()
    {
        m_navAgent.TargetPosition = m_Npc.GlobalPosition;
        m_navAgent.Velocity = Vector3.Zero;
        m_Npc.Velocity = new Vector3(0, m_Npc.Velocity.Y, 0);
    }
}