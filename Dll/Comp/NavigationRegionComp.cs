using Godot;
using System.Collections.Generic;
using System.Linq;
using 途畔归所.Dll.Utils;

public partial class NavigationRegionComp : NavigationRegion3D
{
	private NavigationMesh originalNavMesh;
	private List<Vector3[]> obstacleVerticesList = new();   // 世界空间底面轮廓
	private List<Vector3[]> pendingObstacleVertices = new();
	private bool navMeshReady = false;

	public override void _EnterTree()
	{
		if (NavigationMesh == null)
		{
			CatLog.Err("[NavRegionComp] NavigationMesh 为空，请先烘焙导航网格");
			return;
		}

		originalNavMesh = (NavigationMesh)NavigationMesh.Duplicate(true);
		navMeshReady = true;
		CatLog.Ok($"[NavRegionComp] 原始导航网格已保存，顶点数: {originalNavMesh.GetVertices().Length}, 多边形数: {originalNavMesh.GetPolygonCount()}");

		if (pendingObstacleVertices.Count > 0)
		{
			foreach (var verts in pendingObstacleVertices)
				obstacleVerticesList.Add(verts);
			pendingObstacleVertices.Clear();
			ApplyObstacles();
		}
	}

	public void RegisterObstacle(Vector3[] worldVertices)
	{
		if (!navMeshReady)
		{
			pendingObstacleVertices.Add(worldVertices);
			CatLog.Warn("[NavRegionComp] 导航网格未就绪，障碍物已加入等待队列");
			return;
		}

		obstacleVerticesList.Add(worldVertices);
		ApplyObstacles();
	}

	private void ApplyObstacles()
	{
		if (originalNavMesh == null) return;

		NavigationMesh modified = (NavigationMesh)originalNavMesh.Duplicate(true);
		Vector3[] originalVerts = modified.GetVertices();
		List<Vector3> dynamicVertices = new List<Vector3>(originalVerts);
		List<int[]> allPolygons = new List<int[]>();

		// 保留原有所有多边形
		int polyCount = modified.GetPolygonCount();
		for (int i = 0; i < polyCount; i++)
			allPolygons.Add(modified.GetPolygon(i));

		Transform3D inverse = GlobalTransform.AffineInverse();

		foreach (Vector3[] worldVerts in obstacleVerticesList)
		{
			// 1. 转换到局部坐标并抬升到导航网格表面高度
			Vector3[] localVerts = new Vector3[worldVerts.Length];
			for (int i = 0; i < worldVerts.Length; i++)
			{
				Vector3 local = inverse * worldVerts[i];
				local.Y = GetNavMeshHeightAt(local.X, local.Z, originalVerts);
				localVerts[i] = local;
			}

			// 2. 顶点去重并获取索引
			int[] vertIndices = new int[localVerts.Length];
			for (int i = 0; i < localVerts.Length; i++)
				vertIndices[i] = FindOrAddVertex(dynamicVertices, localVerts[i]);

			// 3. 2D投影并三角化
			Vector2[] poly2D = new Vector2[localVerts.Length];
			for (int i = 0; i < localVerts.Length; i++)
				poly2D[i] = new Vector2(localVerts[i].X, localVerts[i].Z);

			// 转为普通 int[] 避免 Count 属性歧义
			int[] triIndices = Geometry2D.TriangulatePolygon(poly2D).ToArray();
			if (triIndices.Length < 3 || triIndices.Length % 3 != 0)
			{
				CatLog.Warn("[NavRegionComp] 障碍物底面三角化失败，多边形可能无效或缠绕顺序错误");
				continue;
			}

			// 4. 每三个索引生成一个三角形并添加
			for (int t = 0; t < triIndices.Length; t += 3)
			{
				int i0 = triIndices[t];
				int i1 = triIndices[t + 1];
				int i2 = triIndices[t + 2];
				allPolygons.Add(new int[] { vertIndices[i0], vertIndices[i1], vertIndices[i2] });
			}
		}

		CatLog.Ok($"[NavRegionComp] 障碍物多边形已合并，当前总多边形数: {allPolygons.Count}");

		// 写回网格
		modified.SetVertices(dynamicVertices.ToArray());
		modified.ClearPolygons();
		foreach (int[] poly in allPolygons)
			modified.AddPolygon(poly);

		NavigationMesh = modified;
		Enabled = false;
		CallDeferred(nameof(Reactivate));
	}

	private float GetNavMeshHeightAt(float x, float z, Vector3[] navVertices)
	{
		float bestDist = float.MaxValue;
		float bestY = 0f;
		foreach (var v in navVertices)
		{
			float dx = v.X - x;
			float dz = v.Z - z;
			float dist = dx * dx + dz * dz;
			if (dist < bestDist)
			{
				bestDist = dist;
				bestY = v.Y;
			}
		}
		return bestY;
	}

	private int FindOrAddVertex(List<Vector3> vertices, Vector3 vertex)
	{
		const float tolerance = 0.001f;
		for (int i = 0; i < vertices.Count; i++)
		{
			if (vertices[i].DistanceSquaredTo(vertex) < tolerance * tolerance)
				return i;
		}
		vertices.Add(vertex);
		return vertices.Count - 1;
	}

	private void Reactivate()
	{
		Enabled = true;
		CatLog.Ok("[NavRegionComp] 导航区域已重新启用");
	}
}
