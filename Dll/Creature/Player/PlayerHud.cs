using Godot;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

[GlobalClass]
public partial class PlayerHud : Node
{
    private Player m_player;

    private CanvasLayer m_CanvasLayer;

    private bool _IsOwner;

    public override void _Ready()
    {
        var node = GetParent();

        if (node is not Player pl)
        {
            CatLog.Err($"[PlayerHud._Ready]：检测挂载对象并非 Player ，已销毁");
            CatUtils.StopAndExit(this);
            return;
        }

        m_player = pl;

        foreach (var comp in pl.GetChildren())
        {
            if (comp is CanvasLayer canvasLayer) m_CanvasLayer = canvasLayer;
            if (comp is NetSyncBase netSync) _IsOwner = netSync.IsOwner;
        }

        if (_IsOwner == false)
        {
            CatLog.Err($"[PlayerUIHandler._Ready]：非所有组件，已销毁");
            CatUtils.StopAndExit(this);
            return;

        }

        if (m_CanvasLayer == null)
        {
            CatLog.Err($"[PlayerUIHandler._Ready]：检测Player对象未有canvasLayer组件，已销毁");
            CatUtils.StopAndExit(this);
            return;
        }

        var ui = UIManager.Instance.GetUI("hud");

        if (ui is not HudComp hudcomp)
        {
            CatLog.Err($"[PlayerUIHandler._Ready]：获取的 HUD 对象挂载脚本不是 HudComp，已销毁");
            CatUtils.StopAndExit(this);
            return;
        }

        hudcomp.m_maxHP = m_player.m_Health;
        ui.Visible = true;
        m_CanvasLayer.AddChild(ui);
    }

    public override void _Process(double delta)
    {




    }
}
