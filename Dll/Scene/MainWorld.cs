using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using static 途畔归所.Dll.Core.GameCore;

public partial class MainWorld : Node3D
{
    [Export] public Node3D SpawnPian;

    private readonly GameCore.SceneType _sceneType = SceneType.MainWorld;
    public override void _Ready()
    {
        GameCore.Instance.SetCurrentScene(_sceneType);

        PlayerManager.Instance.SpawnLocalPlayer(SpawnPian.GlobalPosition);







    }
}