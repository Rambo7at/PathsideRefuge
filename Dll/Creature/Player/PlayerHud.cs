using Godot;
using System;
using 途畔归所.Dll.Utils;

public partial class PlayerHud : Node
{
    private Player m_Player;

    private CanvasLayer m_CanvasLayer;

    public override void _Ready()
	{

        var node = GetParent();
        if (node == null)
        {
            CatLog.Err($"[PlayerHud._Ready]：检测挂载对象是空，已返回");
            QueueFree();
            return;
        }
        if (node is not Player pl)
        {
            CatLog.Err($"[PlayerHud._Ready]：检测挂载对象并非 player ，已返回");
            QueueFree();
            return;
        }

        m_Player = pl;

        foreach (var comp in pl.GetChildren())
        {
            if (comp is not CanvasLayer canvasLayer) continue;
            if (canvasLayer == null)
            {
                CatLog.Err($"[PlayerUIHandler._Ready]：检测player对象未有canvasLayer组件，已返回");
                QueueFree();
                return;
            }

            m_CanvasLayer = canvasLayer;
        }

    }

	public override void _Process(double delta)
	{
	}
}
