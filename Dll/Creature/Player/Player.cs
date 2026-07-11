using Godot;
using 维修公司.Dll.Interface;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Creature.StateMachine;

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

	public override void _PhysicsProcess(double delta) => CheckRaycastInteract();




	/// <summary> 注：视线射线检测交互对象 </summary>
	public void CheckRaycastInteract()
	{
		if (!m_Eye.IsColliding()) return;

		if (m_Eye.GetCollider() is not IInteractable itemComp) return;

		itemComp.PlayerInteract(Input.IsActionJustPressed("cat_E"), Input.IsActionJustPressed("cat_F"), this);
	}



	private void InitPlayerController()
	{
        m_PlayerController??= new PlayerController();

		//m_PlayerController.Init(this);

		AddChild(m_PlayerController);
    }






}
