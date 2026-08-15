using Godot;
using 维修公司.Dll.data;
using 维修公司.Dll.Interface;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using 途畔归所.Dll.View;
namespace 途畔归所.Dll.Creature;

public partial class Player : Humanoid, IInventoryHolder
{
	// 组合组件
	public PlayerController Controller;
	public PlayerGUI GUI;

	// UI 组件
	public InventoryView InventoryView;
	public ConsoleView ConsoleView;
	public EscView EscView;
	public HudView HudView;
	public DialogView DialogView;


	public override bool IsOwner => m_NetSyncBase != null && m_NetSyncBase.NetID.PeerID == m_NetSyncBase.LocalPeer;


    public override void _Ready()
	{
        CatLog.Debug($"[Player._Ready] 进入，IsOwner={IsOwner}，树状态：{IsInsideTree()}");
        base._Ready();

        if (!IsOwner)
		{
			CatLog.Net("[Player._Ready]：当前并非本地玩家，已关闭运行逻辑");
			SetProcess(false);
			SetPhysicsProcess(false);
			return;
		}

        CatLog.Ok("[Player._Ready] 本地玩家初始化开始");

        InitInventory();
		InitConsole();
		InitEsc();
		InitHUD();
		InitDialog();

		Controller ??= new PlayerController();
		GUI ??= new PlayerGUI();

		AddChild(Controller);
		AddChild(GUI);


	}

	public override void _PhysicsProcess(double delta) => RaycastInteract();


    public override void _ExitTree()
    {
        base._ExitTree();
        PlayerManager.Instance.SaveLocalPlayerData();

        if (NetCore.Instance.IsHost && IsOwner && m_NetSyncBase.NetObj != null)
        {
            NetObjectRegistry.Instance.BroadcastDestroyNetObject(m_NetSyncBase.NetObj);
        }
    }

    /// <summary>注：视线射线检测交互对象</summary>
    public void RaycastInteract()
	{
		if (Input.IsActionJustPressed("cat_E"))
		{
			var cam = WorldManager.Instance.GetCamera();
			if (cam == null) return;

			var viewport = cam.GetViewport();
			var screenCenter = viewport.GetVisibleRect().Size / 2;

			var from = cam.ProjectRayOrigin(screenCenter);
			var dir = cam.ProjectRayNormal(screenCenter);
			var to = from + dir * 10f;

			var spaceState = GetWorld3D().DirectSpaceState;
			SetPhysicsRay(from, to, m_SelfExclude);

			var result = spaceState.IntersectRay(m_PhysicsRay);
			if (result.TryGetValue("collider", out var node) && node.As<Node3D>() is IInteractable i)
			{
				CatLog.Ok($"已发现{i.ObjectName}");
				i.PlayerInteract(true, false, this);
			}
		}
	}

	public DialogView GetDialogView() => DialogView == null ? null : DialogView;







	/// <summary>注：初始化背包视图</summary>
	private void InitInventory()
	{
		if (GUIManager.Instance.GetView(InventoryData.m_UIname) is not InventoryView view)
		{
			CatLog.Err("[Player.InitInventory] 背包视图加载失败");
			return;
		}
		InventoryView = view;
		InventoryView.m_holder = this;
		InventoryView.Visible = false;
	}

	/// <summary>注：初始化控制台视图</summary>
	private void InitConsole()
	{
		if (ConsoleView != null) return;

		if (GUIManager.Instance.GetView("ConsoleView") is not ConsoleView view) return;

		ConsoleView = view;
		ConsoleView.GetPlayer(this);
		ConsoleView.Visible = false;
	}

	/// <summary>注：初始化ESC菜单视图</summary>
	private void InitEsc()
	{
		if (EscView != null) return;

		if (GUIManager.Instance.GetView("EscView") is not EscView view) return;

		EscView = view;
		EscView.Visible = false;
	}

	/// <summary>注：初始化HUD视图</summary>
	private void InitHUD()
	{
		if (HudView != null) return;

		if (GUIManager.Instance.GetView("HudView") is not HudView view) return;

		HudView = view;
		HudView.m_maxHP = m_Health;
		HudView.Visible = true;
	}

	/// <summary>注：初始化对话视图</summary>
	private void InitDialog()
	{
		if (DialogView != null) return;

		if (GUIManager.Instance.GetView("DialogView") is not DialogView view)
		{
			CatLog.Warn("[Player.InitDialog] 对话视图加载失败");
			return;
		}

		DialogView = view;
		DialogView.Visible = false;
	}


	#region 接口实现

	public InventoryData InventoryData
	{
		get => m_CreatureData.InventoryData;
		set => m_CreatureData.InventoryData = value;
	}

	public bool TrySetInventoryItem(int index, ItemData data)
	{
		if (index < 0 || index >= InventoryData.m_capacity) return false;
		InventoryData.m_itemArr[index] = data;
		return true;
	}

	#endregion

}
