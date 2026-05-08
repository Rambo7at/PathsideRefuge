using Godot;
using System;
using System.Linq;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Manager;

/// <summary> 注：NPC 实体类。当前仅用于测试导航巡逻 </summary>
public partial class Npc : Humanoid
{
	private enum NpcState { Patrol, Chase }
	private NpcState m_currentState = NpcState.Patrol;

	[Export] public NpcData m_NpcData;
	[Export] public NavigationAgent3D m_NavigationAgent3D;


	private Vector3 m_NavPatrolTarget { get => m_NavigationAgent3D.TargetPosition; set => m_NavigationAgent3D.TargetPosition = value; }
	private float m_NavStopTimer = 0f;
	private CreatureBase m_creature;

	public override void _Ready()
	{
		if (!Init()) return;
		TestNavigationAvailability();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		See();
		ApplyGravity(delta);

		UpdateStateMachine(dt);

		ApplyMovementToTarget();
		MoveAndSlide();

		FaceMovementOrTarget(m_NavPatrolTarget, m_NpcData.m_rotationSpeed, dt);
	}

	private bool Init()
	{
		if (m_NpcData == null)
		{
			GD.PrintErr("[Npc] m_NpcData 为空");
			return false;
		}

		if (m_NavigationAgent3D != null)
		{
			m_NavigationAgent3D.TargetDesiredDistance = m_NpcData.m_targetDistance;
			m_NavigationAgent3D.Radius = 0.5f;
			m_NavigationAgent3D.Height = 1.8f;
			m_NavigationAgent3D.AvoidanceEnabled = false;
		}

		GD.Print($"[Npc] 初始化完成, 速度={m_NpcData.m_speed}");
		return true;
	}

	private void TestNavigationAvailability()
	{
		if (m_NavigationAgent3D == null) return;
		var map = m_NavigationAgent3D.GetNavigationMap();
		GD.Print(map.IsValid ? "[Npc] 导航地图有效" : "[Npc] 无导航地图");
	}



	/// <summary> 注：状态机调度 </summary>
	private void UpdateStateMachine(float delta)
	{

		switch (m_currentState)
		{
			case NpcState.Patrol:
				UpdatePatrolLogic(delta);

				break;
			case NpcState.Chase:
				UpdateChaseLogic();
				break;
		}

	}



	/// <summary> 注：驱动 NPC 直线移向 m_NavPatrolTarget </summary>
	private void ApplyMovementToTarget()
	{
		if (!IsOnFloor()) return;

		// 优先使用导航路径上的下一个点
		Vector3 targetPoint;
		if (!m_NavigationAgent3D.IsNavigationFinished())
		{
			targetPoint = m_NavigationAgent3D.GetNextPathPosition();
		}
		else
		{
			targetPoint = m_NavPatrolTarget;  // 已到终点，原地停
		}

		Vector3 dir = targetPoint - GlobalPosition;

		if (dir.Length() <= m_NpcData.m_targetDistance)
		{
			MoveHorizontally(Vector3.Zero, m_NpcData.m_speed);
		}
		else
		{
			MoveHorizontally(dir.Normalized(), m_NpcData.m_speed);
		}
			
	}


	/// <summary> 注：巡逻主逻辑，停留计时与到达检测 </summary>
	private void UpdatePatrolLogic(float delta)
	{
		if (m_NavStopTimer > 0f)
		{
			m_NavStopTimer -= delta;
			if (m_NavStopTimer <= 0f)
			{
				m_NavStopTimer = 0f;
				GenerateNavPatrolTarget();
			}
			return;
		}

		// 是否已到达最终目标
		if (m_NavigationAgent3D.IsNavigationFinished())
		{
			m_NavStopTimer = m_NpcData.m_stopTime;
			//GD.Print($"[Npc] 到达巡逻点，停留 {m_NavStopTimer}s");
		}
	}

	/// <summary> 注：在导航网格上随机选点，存入 m_NavPatrolTarget，并设置导航代理目标 </summary>
	private void GenerateNavPatrolTarget()
	{
		if (m_NavigationAgent3D == null) return;

		Vector3 origin = GlobalPosition;
		float radius = m_NpcData.m_patrolRadius;
		int maxAttempts = 15;

		for (int i = 0; i < maxAttempts; i++)
		{
			float angle = (float)GD.RandRange(0, Mathf.Pi * 2);
			float dist = (float)GD.RandRange(1.0f, radius);
			Vector3 candidate = origin + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * dist;

			Rid map = m_NavigationAgent3D.GetNavigationMap();

			Vector3 closest = NavigationServer3D.MapGetClosestPoint(map, candidate);

			float d = closest.DistanceTo(origin);
			if (d <= radius && d > m_NpcData.m_targetDistance * 1.5f)
			{
				m_NavPatrolTarget = closest;
				//GD.Print($"[Npc] 新巡逻目标：{m_NavPatrolTarget}");
				return;
			}
		}

		m_NavStopTimer = m_NpcData.m_stopTime;
		GD.Print("[Npc] 未找到合适点，原地停留");
	}


	private void See()
	{
		if (m_eye.IsColliding() == false || m_creature != null) return;

		var TT = m_eye.GetCollider();

		if (TT is not Player pl) return;

		m_creature = pl;
		m_currentState = NpcState.Chase;
		GD.Print("测试:发现玩家辣！");
	}



	/// <summary> 注：更新追击目标为第一个有效玩家的位置。</summary>
	/// <param name="delta"></param>
	private void UpdateChaseLogic()
	{
		if (m_creature == null) return;

		if (!IsInstanceValid(m_creature))
		{
			m_creature = null;
			m_currentState = NpcState.Patrol;
			return;
		}

		Rid map = m_NavigationAgent3D.GetNavigationMap();        
		m_NavPatrolTarget = NavigationServer3D.MapGetClosestPoint(map, m_creature.GlobalPosition);
		m_NavStopTimer = 0f;
	}



}
