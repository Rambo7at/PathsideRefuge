using Godot;
using 维修公司.Dll.Interface;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

public partial class Player : Humanoid
{
    [Export] public PlayerGUI m_PlayerGUI;
    [Export] public Node3D m_PlayerModel;






    private PlayerController m_PlayerController;




    public override void _EnterTree()
    {
        // 执行父类 _EnterTree
        base._EnterTree();









    }






    public override void _Ready()
    {
        // 执行父类 _Ready
        base._Ready();


        if (m_IsOwner == true)
        {
            //InitPlayerController();


        }

        if (m_PlayerGUI == null || m_PlayerModel == null)
        {
            string loga = m_PlayerGUI == null ? "m_PlayerGUI" : string.Empty;
            string logb = m_PlayerModel == null ? "m_PlayerModel" : string.Empty;
            CatLog.Net($"[Player._Ready]：{loga}/{logb} 字段为空");
            CatUtils.StopAndExit(this);
        }

        if (!m_IsOwner)
        {
            CatLog.Net($"[Player._Ready]：当前并非本地玩家，已关闭运行逻辑");
            SetProcess(false);
            SetPhysicsProcess(false);
        }
    }

    public override void _PhysicsProcess(double delta) => RaycastInteract();


    /// <summary> 注：视线射线检测交互对象 </summary>
    public void RaycastInteract()
    {
        if (Input.IsActionJustPressed("cat_E"))
        {
            var cam = WorldManager.Instance.GetCamera();
            if (cam == null) return;
            var viewport = cam.GetViewport();
            var screenCenter = viewport.GetVisibleRect().Size / 2;

            Vector3 from = cam.ProjectRayOrigin(screenCenter);
            Vector3 dir = cam.ProjectRayNormal(screenCenter);
            float distance = 10f;
            Vector3 to = from + dir * distance;

            var spaceState = GetWorld3D().DirectSpaceState;

            SetPhysicsRay(from, to, m_SelfExclude);

            var result = spaceState.IntersectRay(m_PhysicsRay);

            if (result.TryGetValue("collider", out var node))
            {
                if (node.As<Node3D>() is not IInteractable i) return;

                CatLog.Ok($"已发现{i.ObjectName}");
                i.PlayerInteract(true,false,this);
            }
        }
    }



    private void InitPlayerController()
    {
        m_PlayerController ??= new PlayerController();

        //m_PlayerController.Init(this);

        AddChild(m_PlayerController);
    }






}
