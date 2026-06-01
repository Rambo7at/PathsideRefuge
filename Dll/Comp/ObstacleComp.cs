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
		CollisionShape3D col = CatUtils.FindChildNode<CollisionShape3D>(parent);

		if (net?.m_NetObj == null)
		{
			CatLog.Warn("ObstacleComp: 未找到有效 NetSyncBase，跳过");
			return;
		}

		if (col?.Shape == null)
		{
			CatLog.Err($"ObstacleComp: 父节点 {parent?.Name} 下找不到 CollisionShape3D 或形状为空");
			return;
		}

		// 获取导航组件：从场景根或直接通过路径
		Node currentScene = WorldManager.Instance.GetCurrentScene();
		NavigationRegionComp navRegion = currentScene?.GetNodeOrNull<NavigationRegionComp>("NavigationRegion3D");
		if (navRegion == null)
		{
			CatLog.Err("ObstacleComp: 找不到 NavigationRegionComp");
			return;
		}

		ArrayMesh debugMesh = col.Shape.GetDebugMesh();
		Aabb localAabb = debugMesh.GetAabb();
		Aabb worldAabb = col.Transform * localAabb;

		CatLog.Info($"ObstacleComp: 准备上报，局部AABB: {localAabb}, 世界AABB: {worldAabb}");
		navRegion.RegisterObstacle(worldAabb);
	}
}
