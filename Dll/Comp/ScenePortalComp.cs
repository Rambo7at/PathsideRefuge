using Godot;
using 维修公司.Dll.Interface;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Manager;

namespace 途畔归所.Dll.Comp;

[GlobalClass]
public partial class ScenePortalComp : Node3D, IInteractable
{

    [ExportGroup("传送门配置")]
    [Export] private Area3D triggerArea;        // Inspector 显示为"触发区域"
    [Export] private string targetScene;        // Inspector 显示为"目标场景"

    public string ObjectName => "传送门";

    public override void _Ready()
    {
        //if (triggerArea == null || string.IsNullOrEmpty(targetScene))
        //{
        //    return;
        //}

        //triggerArea.BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not Player player) return;
        if (!player.IsOwner) return;  // 只有本地玩家能触发

        CallDeferred(nameof(DoChangeScene));
    }

    private void DoChangeScene()
    {
        WorldManager.Instance.ChangeScene(targetScene);
    }

    public void PlayerInteract(bool InputE, bool InputF, CreatureBase creature)
    {
        if (creature is not Player) return;
        if (InputE)
        {
            WorldManager.Instance.ChangeScene(targetScene);
        }
       

    }
}

