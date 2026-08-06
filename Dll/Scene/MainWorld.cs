using Godot;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Manager;

namespace 途畔归所.Dll.Scene;

public partial class MainWorld : SceneBase
{
	[Export] public Node3D SpawnPian;

	public override void _Ready()
	{

		PlayerManager.Instance.SpawnLocalPlayer(SpawnPian.GlobalPosition, SpawnPian.Rotation);
	}
}
