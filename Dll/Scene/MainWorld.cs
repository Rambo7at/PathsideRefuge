using Godot;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Manager;

public partial class MainWorld : SceneBase
{
    [Export] public Node3D SpawnPian;

    public override void _Ready()
    {
       
        PlayerManager.Instance.SpawnLocalPlayer(SpawnPian.GlobalPosition, SpawnPian.Rotation);
    }
}