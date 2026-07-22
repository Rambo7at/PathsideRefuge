using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Creature.StateMachine;
namespace 途畔归所.Dll.Creature.Npc;

public partial class NpcMovement : Node3D
{
    private Npc m_Npc;
    private StateMachine m_StateMachine;
    private NavigationAgent3D m_NavAgent;

    // 便捷属性
    private AnimState AnimState => m_StateMachine.CurrentAnimState;
    private NpcState NpcState => m_StateMachine.CurrentNpcState;
    private bool IsStaggerState => AnimState == AnimState.Stagger;
    private bool IsDeathState => AnimState == AnimState.Death;
    private bool IsAttackState => AnimState == AnimState.Attack;

    private Vector3 m_SafeVelocity = Vector3.Zero;  // 存储 avoidance 后的安全速度

    public override void _Ready()
    {
        if (NetCore.Instance.IsClient)
        {
            SetPhysicsProcess(false);
            return;
        }

        if (GetParent() is not Npc node)
        {
            CatLog.Err($"[NpcMovement._Ready]：挂载对象非 NPC类型 -{GetParent().Name} ，已销毁");
            CatUtils.StopAndExit(this);
            return;
        }

        m_NavAgent = CatUtils.FindChildNode<NavigationAgent3D>(node);

        if (m_NavAgent == null)
        {
            CatLog.Err("[NpcMovement._Ready]：缺少 NavigationAgent3D 组件，已销毁，请检查编辑器");
            CatUtils.StopAndExit(this);
            return;
        }

        m_Npc = node;
        m_StateMachine = node.m_StateMachine;

        // 连接 avoidance 计算结果信号
        m_NavAgent.VelocityComputed += OnVelocityComputed;
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
        // 完成导航停止
        if (IsNavigationFinished())
        {
            m_NavAgent.Velocity = Vector3.Zero;
            m_Npc.Velocity = new Vector3(0, m_Npc.Velocity.Y, 0);
            return;
        }

        // 检查是否处于不可移动状态
        if (m_Npc.IsDead || IsStaggerState || IsDeathState || IsAttackState)
        {
            m_Npc.Velocity = new Vector3(0, m_Npc.Velocity.Y, 0);
            return;
        }

        // 1. 获取下一个路径点，计算期望水平速度
        Vector3 toTarget = m_NavAgent.GetNextPathPosition() - m_Npc.GlobalPosition;
        toTarget.Y = 0;

        Vector3 desiredVelocity = toTarget.Length() > 0.1f ? toTarget.Normalized() * m_Npc.m_Speed : Vector3.Zero;

        // 2. 将期望速度提交给导航代理（触发 avoidance 计算）
        m_NavAgent.Velocity = desiredVelocity;

        // 3. 使用上一帧计算出的安全速度（由信号更新）
        m_Npc.Velocity = new Vector3(m_SafeVelocity.X, m_Npc.Velocity.Y, m_SafeVelocity.Z);

        // 4. 面向移动方向
        if (m_SafeVelocity.LengthSquared() > 0.01f)
        {
            m_Npc.FaceMovementOrTarget(m_SafeVelocity, m_Npc.m_RotationSpeed, delta);
        }
    }

    private void OnVelocityComputed(Vector3 safeVelocity)
    {
        m_SafeVelocity = safeVelocity;
    }

    /// <summary>注：检查导航地图是否已就绪</summary>
    private bool IsNavigationMapReady()
    {
        if (m_NavAgent == null) return false;
        Rid map = m_NavAgent.GetNavigationMap();
        return NavigationServer3D.MapGetIterationId(map) > 0;
    }

    /// <summary>注：设置随机巡逻目标点</summary>
    public void SetRandomPatrolTarget(float radius, float minDistance = 0f)
    {
        if (m_NavAgent == null || !IsNavigationMapReady()) return;

        Vector3 origin = m_Npc.GlobalPosition;
        Rid map = m_NavAgent.GetNavigationMap();

        for (int i = 0; i < 15; i++)
        {
            float angle = (float)GD.RandRange(0, Mathf.Pi * 2);
            float dist = (float)GD.RandRange(1.0f, radius);
            Vector3 candidate = origin + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * dist;
            Vector3 closest = NavigationServer3D.MapGetClosestPoint(map, candidate);

            float d = closest.DistanceTo(origin);
            if (d <= radius && d > minDistance)
            {
                SetNavigation(closest);
                return;
            }
        }

        SetNavigation(origin); // 找不到就原地
    }

    /// <summary>注：检查导航是否已完成</summary>
    public bool IsNavigationFinished() => m_NavAgent.IsNavigationFinished();

    /// <summary>注：设置导航目标点</summary>
    public void SetNavigation(Vector3 target)
    {
        if (!IsNavigationMapReady()) return;

        Rid map = m_NavAgent.GetNavigationMap();
        Vector3 targetOnNav = NavigationServer3D.MapGetClosestPoint(map, target);
        m_NavAgent.TargetPosition = targetOnNav;
    }

    /// <summary>注：清除导航目标，停止移动</summary>
    public void ClearNavigation()
    {
        if (m_NavAgent == null) return;
        m_NavAgent.TargetPosition = m_Npc.GlobalPosition;
        m_NavAgent.Velocity = Vector3.Zero;
        m_Npc.Velocity = new Vector3(0, m_Npc.Velocity.Y, 0);
    }
}