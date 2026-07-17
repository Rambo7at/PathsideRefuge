using Godot;
using Godot.Collections;
using System.Linq;
using System.Threading.Channels;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Comp
{

	public partial class SenseComp : Area3D
	{
		private CreatureBase m_CreatureBase;
		private CollisionShape3D m_VisionShape;


		private Array<CreatureBase> m_CreatureList = [];
		private Array<ItemComp> m_ItemList = [];


		// 便捷属性
		private Node3D m_Eye => m_CreatureBase.m_Eye;
		private PhysicsRayQueryParameters3D m_PhysicsRay => m_CreatureBase.m_PhysicsRay;

		//测试属性
		private float m_VisionAngle = 90f;

		private float m_VisionDistance = 10f;


		// 公共列表
		public Array<CreatureBase> m_DetectedCreaturesList = [];
		public Array<ItemComp> m_DetectedItemList = [];


		public override void _Ready()
		{
			if (GetParent() is not CreatureBase node)
			{
				CatUtils.StopAndExit(this);
				return;
			}

			m_CreatureBase = node;

			m_VisionShape ??= new CollisionShape3D();

			m_VisionShape.Shape = new SphereShape3D() { Radius = 100 };

			AddChild(m_VisionShape);

			BodyEntered += SenseComp_BodyEntered;
			BodyExited += SenseComp_BodyExited;
		}


		public override void _PhysicsProcess(double delta)
		{
			UpdateVision();
		}

		private void SenseComp_BodyExited(Node3D body)
		{
			if (body is CreatureBase creature)
			{
				m_CreatureList.Remove(creature);
			}
		}

		private void SenseComp_BodyEntered(Node3D body)
		{
			if (body is CreatureBase creature)
			{
				if (m_CreatureList.Contains(creature) || creature == m_CreatureBase) return;
				m_CreatureList.Add(creature);
				CatLog.Ok($"已找到生物添加显示信息{creature.Name}");
			}

			if (body is ItemComp item)
			{
				if (m_ItemList.Contains(item)) return;
				m_ItemList.Add(item);
				CatLog.Ok($"已找到物品添加显示信息{item.Name}");
			}
		}


		private void UpdateVision()
		{
			PerformDetection(m_CreatureList, m_DetectedCreaturesList);
			PerformDetection(m_ItemList, m_DetectedItemList);
		}


		private void PerformDetection<[MustBeVariant] T>(Array<T> list1, Array<T> list2)
		{
			if (list1.Count == 0) return;

			foreach (var obj in list1)
			{
				if (list2.Contains(obj)) continue;
				if (obj is not Node3D node3d) continue;

				var result = PerformRaycast(m_Eye.GlobalPosition, node3d.GlobalPosition, m_CreatureBase.m_SelfExclude);

				if (result?.Count > 0 && result.TryGetValue("collider", out var node))
				{
					if (node.As<T>() is not Node3D detected) continue;

					if (detected == node3d && !list2.Contains(obj))
					{
						list2.Add(obj);
						CatLog.Ok($"已发现{detected.Name}");
					}
				}
			}
		}

		private Dictionary PerformRaycast(Vector3 from ,Vector3 to, Array<Rid> exclude)
		{
			if (from.DistanceTo(to) > m_VisionDistance) return null;


			// 向量点积
			Vector3 forward = -m_Eye.GlobalBasis.Z;
			Vector3 dirToTarget = (to - from).Normalized();
			float halfAngleRad = Mathf.DegToRad(m_VisionAngle * 0.5f);
			if (dirToTarget.Dot(forward) < Mathf.Cos(halfAngleRad)) return null;

			// 发射射线
			var spaceState = GetWorld3D().DirectSpaceState;

			m_CreatureBase.SetPhysicsRay(from,to,exclude);

			return spaceState.IntersectRay(m_PhysicsRay);

		}

	}

}
