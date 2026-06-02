using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Creature.Npc;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Utils;

[GlobalClass]
public partial class NpcMovement : Node3D
{
    [Export] public NavigationAgent3D m_navAgent;

    private Npc _npc;
    private NpcData _npcData;
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

        _npc = node;
        _npcData = _npc.m_NpcData ?? new NpcData();

        // 连接 avoidance 计算结果信号
        m_navAgent.VelocityComputed += OnVelocityComputed;
    }



    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _npc.ApplyGravity(dt);
        ApplyMovement(dt);
        _npc.MoveAndSlide();
    }

    private void ApplyMovement(float delta)
    {
        if (m_navAgent.IsNavigationFinished())
        {
            // 停止
            m_navAgent.Velocity = Vector3.Zero;
            _npc.Velocity = new Vector3(0, _npc.Velocity.Y, 0);
            return;
        }

        // 1. 获取下一个路径点，计算期望水平速度
        Vector3 nextPoint = m_navAgent.GetNextPathPosition();
        Vector3 toTarget = nextPoint - _npc.GlobalPosition;
        toTarget.Y = 0;

        float speed = _npcData.m_speed;
        Vector3 desiredVelocity = toTarget.Length() > 0.1f
            ? toTarget.Normalized() * speed
            : Vector3.Zero;

        // 2. 将期望速度提交给导航代理（触发 avoidance 计算）
        m_navAgent.Velocity = desiredVelocity;

        // 3. 使用上一帧计算出的安全速度（由信号更新）
        _npc.Velocity = new Vector3(_safeVelocity.X, _npc.Velocity.Y, _safeVelocity.Z);

        // 4. 面向移动方向
        if (_safeVelocity.LengthSquared() > 0.01f)
        {
            _npc.FaceMovementOrTarget(_safeVelocity, _npcData.m_rotationSpeed, delta);
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
        m_navAgent.TargetPosition = _npc.GlobalPosition;
        m_navAgent.Velocity = Vector3.Zero;
        _npc.Velocity = new Vector3(0, _npc.Velocity.Y, 0);
    }
}