using Godot;
using 维修公司.Dll;

public partial class Spawn : Node3D
{
    [Export]
    string name = string.Empty;

    public override void _Ready()
    {
        CallDeferred(nameof(SpawnItem));
    }


    private void SpawnItem()
    {
        var x = ItemManager.Instance.GetItemDrop(name);
        GetTree().CurrentScene.AddChild(x);
        x.GlobalPosition = this.GlobalPosition;

    }
}