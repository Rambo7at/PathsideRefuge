using Godot;
using 维修公司.Dll;
using 途畔归所.Dll.Core;

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
        var x = GameCore.Instance.m_ItemManager.GetItemDrop(name);
        GetTree().CurrentScene.AddChild(x);
        x.GlobalPosition = this.GlobalPosition;

    }
}