using Godot;
using System;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Data;

public partial class Npc : Humanoid
{
	[Export] public NpcData m_NpcData;
	[Export] public NavigationAgent3D m_NavigationAgent3D; // 暂未使用

	// 旋转
	private float targetAngle;
	private const float RotationSpeed = 15f;

	// 巡逻状态
	private Vector3 originPoint;
	private Vector3 patrolTarget;
	private float stopTimer;
	private bool isStopping;

	// ──────────── 导航检测（仅测试）─────────────
	private void TestNavigationAvailability()
	{
		if (m_NavigationAgent3D == null)
		{
			GD.PrintErr("[Npc] 没有 NavigationAgent3D 节点，跳过导航检测");
			return;
		}

		// 1. 检查导航地图是否存在
		Rid map = m_NavigationAgent3D.GetNavigationMap();
		if (!map.IsValid)
		{
			GD.PrintErr("[Npc] 当前世界没有可用的导航地图！");
			return;
		}
		GD.Print($"[Npc] 导航地图存在，RID: {map}");

		// 2. 尝试对一个已知可达的目标进行可达性测试
		//    使用自身位置作为测试目标（必定在地面上，如果在导航网格内）
		m_NavigationAgent3D.TargetPosition = GlobalPosition;
		bool reachable = m_NavigationAgent3D.IsTargetReachable();
		GD.Print($"[Npc] 自身位置是否可达: {reachable}");

		if (!reachable)
		{
			// 尝试最近点
			Vector3 closest = NavigationServer3D.MapGetClosestPoint(map, GlobalPosition);
			GD.Print($"[Npc] 距离最近导航点: {closest}, 距离: {GlobalPosition.DistanceTo(closest)}");
		}

		// 3. 获取当前路径，打印前几个点
		var path = m_NavigationAgent3D.GetCurrentNavigationPath();
		GD.Print($"[Npc] 当前路径点数量: {path.Length}");
		for (int i = 0; i < Math.Min(3, path.Length); i++)
		{
			GD.Print($"  路径点 {i}: {path[i]}");
		}

		GD.Print("[Npc] 导航检测完成");
	}

	// ──────────── 主循环 ─────────────
	public override void _Ready()
	{
		if (!TryInitialize()) return;

		originPoint = GlobalPosition;
		GeneratePatrolTarget();
		TestNavigationAvailability();

	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		UpdateGravity(delta);
		UpdatePatrolLogic(dt);
		ApplyMovement();
		MoveAndSlide();
		SmoothRotate(dt);
	}

	// ──────────── 初始化 ─────────────
	private bool TryInitialize()
	{
		if (m_NpcData == null)
		{
			GD.PrintErr("[Npc] m_NpcData 为空");
			return false;
		}
		// 导航代理（暂未使用）的参数设置可保留，以后用
		if (m_NavigationAgent3D != null)
		{
			m_NavigationAgent3D.TargetDesiredDistance = m_NpcData.m_targetDistance;
			m_NavigationAgent3D.Radius = 0.5f;
			m_NavigationAgent3D.Height = 1.8f;
		}
		GD.Print($"[Npc] 初始化完成, 速度={m_NpcData.m_speed}");
		return true;
	}

	// ──────────── 巡逻逻辑 ─────────────
	private void UpdatePatrolLogic(float delta)
	{
		if (!IsOnFloor()) return;   // 空中不执行巡逻逻辑

		if (isStopping)
		{
			stopTimer += delta;
			if (stopTimer >= m_NpcData.m_stopTime)
			{
				isStopping = false;
				stopTimer = 0f;
				GeneratePatrolTarget();
			}
			return;
		}

		float dist = GlobalPosition.DistanceTo(patrolTarget);
		if (dist <= m_NpcData.m_targetDistance)
		{
			isStopping = true;
		}
	}

	private void GeneratePatrolTarget()
	{
		float angle = (float)GD.RandRange(0, Math.PI * 2);
		float dist = (float)GD.RandRange(1, m_NpcData.m_patrolRadius);
		patrolTarget = originPoint + new Vector3(
			Mathf.Cos(angle) * dist,
			0,
			Mathf.Sin(angle) * dist
		);
		patrolTarget.Y = originPoint.Y;  // 保持在同一水平面
	}

	// ──────────── 移动执行 ─────────────
	private void ApplyMovement()
	{
		var vel = Velocity;
		Vector3 direction = Vector3.Zero;

		if (!isStopping && IsOnFloor())
		{
			Vector3 toTarget = patrolTarget - GlobalPosition;
			toTarget.Y = 0;
			if (toTarget.Length() > m_NpcData.m_targetDistance)
			{
				direction = toTarget.Normalized();
			}
		}

		if (direction != Vector3.Zero)
		{
			vel.X = direction.X * m_NpcData.m_speed;
			vel.Z = direction.Z * m_NpcData.m_speed;
			targetAngle = Mathf.Atan2(direction.X, direction.Z);
		}
		else
		{
			vel.X = 0;
			vel.Z = 0;
		}

		Velocity = vel;
	}

	private void SmoothRotate(float delta)
	{
		float newY = Mathf.LerpAngle(GlobalRotation.Y, targetAngle, RotationSpeed * delta);
		GlobalRotation = new Vector3(GlobalRotation.X, newY, GlobalRotation.Z);
	}
}
