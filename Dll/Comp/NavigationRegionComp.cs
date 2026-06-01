using Godot;
using Godot.Collections;
using System.Collections.Generic;
using 途畔归所.Dll.Utils;

public partial class NavigationRegionComp : NavigationRegion3D
{
	private NavigationMesh originalNavMesh;
	private List<Aabb> obstacleAabbs = new();
	private List<Aabb> pendingObstacleAabbs = new();
	private bool navMeshReady = false;

	public override void _EnterTree()
	{
		InitializeNavMesh();
	}

	private void InitializeNavMesh()
	{
		if (NavigationMesh == null)
		{
			CatLog.Err("[NavRegionComp] NavigationMesh 为空，请先在编辑器中烘焙导航网格");
			return;
		}

		originalNavMesh = (NavigationMesh)NavigationMesh.Duplicate(true);
		navMeshReady = true;

		CatLog.Ok($"[NavRegionComp] 原始导航网格已保存，顶点数: {originalNavMesh.GetVertices().Length}, 多边形数: {originalNavMesh.GetPolygonCount()}");

		if (pendingObstacleAabbs.Count > 0)
		{
			CatLog.Info($"[NavRegionComp] 处理 {pendingObstacleAabbs.Count} 个提前排队的障碍物");
			foreach (Aabb aabb in pendingObstacleAabbs)
			{
				obstacleAabbs.Add(aabb);
			}
			pendingObstacleAabbs.Clear();
			ApplyObstacles();
		}
	}

	public void RegisterObstacle(Aabb worldAabb)
	{
		CatLog.Info($"[NavRegionComp] 注册障碍物，世界AABB: {worldAabb}");

		if (!navMeshReady)
		{
			CatLog.Warn("[NavRegionComp] 导航网格尚未初始化，障碍物加入等待队列");
			pendingObstacleAabbs.Add(worldAabb);
			return;
		}

		obstacleAabbs.Add(worldAabb);
		ApplyObstacles();
	}

	public void UnregisterObstacle(Aabb worldAabb)
	{
		if (obstacleAabbs.Remove(worldAabb))
		{
			CatLog.Info($"[NavRegionComp] 移除障碍物，世界AABB: {worldAabb}");
			ApplyObstacles();
		}
	}

	private void ApplyObstacles()
	{
		if (originalNavMesh == null)
		{
			CatLog.Err("[NavRegionComp] ApplyObstacles: originalNavMesh 为空");
			return;
		}

		NavigationMesh modified = (NavigationMesh)originalNavMesh.Duplicate(true);
		Vector3[] vertices = modified.GetVertices();
		int polyCount = modified.GetPolygonCount();

		CatLog.Debug($"[NavRegionComp] 深拷贝完成，顶点数: {vertices.Length}, 多边形数: {polyCount}");

		if (vertices.Length == 0 || polyCount == 0)
		{
			CatLog.Warn("[NavRegionComp] 网格无顶点或多边形，跳过裁剪");
			return;
		}

		// 世界 AABB → 局部坐标
		Transform3D inverseTransform = GlobalTransform.AffineInverse();
		List<Aabb> localAabbs = new();
		foreach (Aabb worldAabb in obstacleAabbs)
		{
			Aabb localAabb = inverseTransform * worldAabb;
			localAabbs.Add(localAabb);
			CatLog.Debug($"[NavRegionComp] 障碍物 世界 {worldAabb} → 局部 {localAabb}");
		}

		// 过滤多边形：只要多边形的包围盒与任何障碍物 AABB 相交，就移除
		List<int[]> keptPolygons = new();
		int removedCount = 0;

		for (int i = 0; i < polyCount; i++)
		{
			int[] poly = modified.GetPolygon(i);
			if (poly.Length == 0) continue;

			// 计算多边形的 AABB
			Aabb polyAabb = new Aabb(vertices[poly[0]], Vector3.Zero);
			for (int j = 1; j < poly.Length; j++)
			{
				polyAabb = polyAabb.Expand(vertices[poly[j]]);
			}

			// 检测是否与任何障碍物 AABB 相交
			bool intersects = false;
			foreach (Aabb obstacleAabb in localAabbs)
			{
				if (polyAabb.Intersects(obstacleAabb))
				{
					intersects = true;
					break;
				}
			}

			if (intersects)
			{
				removedCount++;
			}
			else
			{
				keptPolygons.Add(poly);
			}
		}

		CatLog.Ok($"[NavRegionComp] 裁剪结果: 移除 {removedCount} 个多边形, 保留 {keptPolygons.Count} 个");

		modified.ClearPolygons();
		foreach (int[] poly in keptPolygons)
		{
			modified.AddPolygon(poly);
		}

		NavigationMesh = modified;
		Enabled = false;
		CallDeferred(nameof(Reactivate));
	}

	private void Reactivate()
	{
		Enabled = true;
		CatLog.Ok("[NavRegionComp] 导航区域已重新启用");
	}
}
