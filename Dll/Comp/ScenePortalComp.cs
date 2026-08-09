using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Comp;

[GlobalClass]
public partial class ScenePortalComp : Node3D
{

    [ExportGroup("传送门配置")]
    [Export] private Area3D triggerArea;        // Inspector 显示为"触发区域"
    [Export] private string targetScene;        // Inspector 显示为"目标场景"


    public override void _Ready()
    {
        if (triggerArea == null || string.IsNullOrEmpty(targetScene))
        {


            return;
        }

       

        triggerArea.BodyEntered += OnBodyEntered;

    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not Player player) return;
        if (!player.m_IsOwner) return;  // 只有本地玩家能触发

        CallDeferred(nameof(DoChangeScene));
    }

    private void DoChangeScene()
    {
        WorldManager.Instance.ChangeScene(targetScene);
    }
}

