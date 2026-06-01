using Godot;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

[GlobalClass]
public partial class ObstacleComp : NavigationObstacle3D
{
	public override void _Ready()
	{
		Node parent = GetParent();
		NetSyncBase net = CatUtils.FindChildNode<NetSyncBase>(parent);
		if (net?.m_NetObj == null) return;

		NavigationRegionComp navRegion = CatUtils.FindChildNode<NavigationRegionComp>(WorldManager.Instance.GetCurrentScene());
		if (navRegion == null)
		{
			CatLog.Err("ObstacleComp: 找不到 NavigationRegionComp");
			return;
		}

		// 获取手绘的底面轮廓顶点（局部坐标）
		Vector3[] localVerts = Vertices;
		if (localVerts == null || localVerts.Length < 3)
		{
			CatLog.Err("ObstacleComp: 顶点不足，无法构成多边形");
			return;
		}

		// 转换为世界坐标
		Node3D crate = parent as Node3D;
		if (crate == null)
		{
			CatLog.Err("ObstacleComp: 父节点不是 Node3D");
			return;
		}

		Vector3[] worldVerts = new Vector3[localVerts.Length];
		for (int i = 0; i < localVerts.Length; i++)
			worldVerts[i] = crate.Transform * localVerts[i];

		// 上报给导航区域组件
		navRegion.RegisterObstacle(worldVerts);
		CatLog.Info($"ObstacleComp: 已上报障碍物顶点，数量: {worldVerts.Length}");
	}
}
