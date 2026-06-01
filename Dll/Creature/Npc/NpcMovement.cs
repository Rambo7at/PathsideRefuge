using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Creature.Npc;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Utils;



/// <summary>注：Npc移动组件 </summary>
[GlobalClass]
public partial class NpcMovement : Node3D
{
    [Export] public NavigationAgent3D m_navAgent;

    private Npc _npc;
    private NpcData _npcData;

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

        m_navAgent.Radius = 0.5f;
        m_navAgent.Height = 1.8f;
       
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _npc.ApplyGravity(dt);
        ApplyMovement(dt);
        _npc.MoveAndSlide();
    }

    /// <summary>注：执行移动 </summary>
    private void ApplyMovement(float delta)
    {
        if (m_navAgent.IsNavigationFinished())
        {
            _npc.MoveHorizontally(_npc.GlobalPosition, 0);
            return;
        }

        Vector3 nextPoint = m_navAgent.GetNextPathPosition();

        _npc.MoveHorizontally(nextPoint, _npcData.m_speed);
        _npc.FaceMovementOrTarget(nextPoint, _npcData.m_rotationSpeed, delta);
    }

    /// <summary>注：设置导航目标 </summary>
    public void SetNavigation(Vector3 target)
    {
        CatLog.Ok($"设置目标导航点{target}");
        m_navAgent.TargetPosition = target;
    }

    /// <summary>注：清除导航目标并停止移动。 </summary>
    public void ClearNavigation()
    {
        m_navAgent.TargetPosition = _npc.GlobalPosition;
        _npc.MoveHorizontally(_npc.GlobalPosition, 0);
    }
}