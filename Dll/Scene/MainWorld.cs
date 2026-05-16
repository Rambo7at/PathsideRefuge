using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;

public partial class MainWorld : Node3D
{
    [Export] public Node3D SpawnPian;

    private readonly GameCore.SceneType _sceneType = GameCore.SceneType.MainWorld;
    public override void _Ready()
    {
        GameCore.Instance.SetCurrentSceneType(_sceneType,this);

        GameCore.Instance.SetCurrentScene(this);

        PlayerManager.Instance.SpawnLocalPlayer(SpawnPian.GlobalPosition, SpawnPian.Rotation);
    }
}