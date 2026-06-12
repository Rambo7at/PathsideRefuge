using Godot;
using 维修公司.Dll.data;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Comp;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

public partial class Player : Humanoid
{
	[Export] public Node3D m_PlayerModel;
	[Export] public PlayerGUI m_playerGUI;

	[Export] public BoneAttachment3D m_HandL;
	[Export] public BoneAttachment3D m_HandR;

	public bool m_OnUI = false;

	public CreatureData m_data;
	private NetSyncBase netSync;
	public bool m_IsOwner => netSync != null && netSync.IsOwner;



	public override void _EnterTree()
	{
		netSync = CatUtils.FindChildNode<NetSyncBase>(this);

		if (netSync == null)
		{
			CatLog.Err($"[Player._EnterTree]：未找到NetSyncBase网络同步组件，已关闭运行逻辑");
			SetProcess(false);
			SetPhysicsProcess(false);
			return;
		}

		if (!m_IsOwner)
		{
			CatLog.Net($"[Player._EnterTree]：当前并非本地玩家，已关闭运行逻辑");
			SetProcess(false);
			SetPhysicsProcess(false);
		}
	}

	public override void _Ready()
	{
		if (!ValidateComponents()) return;
	}





	public override void _Process(double delta)
	{

	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsInsideTree()) return;
		CheckRaycastInteract();
	}

	public void Equip(ItemData itemData)
	{
		var drop = itemData.DataToDrop();
		if (drop == null) return;

		var item = drop as ItemComp;
		m_HandR.AddChild(item);
	}

	/// <summary> 注：视线射线检测交互对象 </summary>
	public void CheckRaycastInteract()
	{
		if (!m_eye.IsColliding()) return;

		var ojb = m_eye.GetCollider();

		if (ojb == null) return;

		if (ojb is ItemComp itemComp)
		{
			itemComp.PlayerInteract(Input.IsActionJustPressed("cat_E"), Input.IsActionJustPressed("cat_F"), this);
		}
		else if (ojb is ContainerComp containerComp)
		{
			containerComp.PlayerInteract(Input.IsActionJustPressed("cat_E"), Input.IsActionJustPressed("cat_F"), this);
		}
	}





	/// <summary> 注：验证所有关键组件是否非空 </summary>
	private bool ValidateComponents()
	{
		if (m_eye == null)
		{
			GD.PrintErr("[Player.ValidateComponents]：m_eye 字段为空");
			return false;
		}
		if (m_PlayerModel == null)
		{
			GD.PrintErr("[Player.ValidateComponents]：m_PlayerModel 字段为空");
			return false;
		}
		if (m_playerGUI == null)
		{
			GD.PrintErr("[Player.ValidateComponents]：m_CanvasLayer 字段为空");
			return false;
		}
		if (m_AnimationTree == null)
		{
			GD.PrintErr("[Player.ValidateComponents]：m_AnimationTree 字段为空");
			return false;
		}
		return true;
	}
}
