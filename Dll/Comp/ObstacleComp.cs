using Godot;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

[GlobalClass]
public partial class ObstacleComp : NavigationObstacle3D
{
	public override void _Ready()
	{
		NetSyncBase net = CatUtils.FindChildNode<NetSyncBase>(GetParent());
		if (net?.m_NetObj == null) return;
	}
}
