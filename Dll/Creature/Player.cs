using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using 维修公司.Dll.data;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Comp;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;

public partial class Player : Humanoid, IInventoryHolder
{
	[Export] public Camera3D m_Camera;
	[Export] public Node3D m_PlayerModel;
	[Export] public Control m_PickUpUI;
	[Export] public CanvasLayer m_CanvasLayer;

	public string PlayerName { get => m_PlayerData.m_Name; }
	private bool m_IsPlayerMenu = false;
	public bool m_OnUI = false;


	public PlayerData m_PlayerData;
	public InventoryComp m_InventoryComp;
	public ConsoleComp m_ConsoleComp;
	public EscComp m_EscComp;
	private PlayerController m_Controller;
	public PlayerAnimKeys m_PlayerAnimKeys;
	public List<ItemComp> m_InRangeItems = new List<ItemComp>();


	public override void _Ready()
	{
		m_PickUpUI.Visible = false;
		if (!ValidateComponents()) return;
		InitPlayerAnimKeys();
		InitPlayerController();
		InitInventory();
		InitConsole();
		InitEsc();
	}
	public override void _Process(double delta)
	{
		m_Controller.Update(delta);
		ProcessUIInputs();
		UpdateMouseMode();
		

    }
	public override void _PhysicsProcess(double delta)
	{
		if (!IsInsideTree()) return;
		m_Controller.PhysicsUpdate(delta);
        Test();
    }

    public void Test()
    {
        if (!m_eye.IsColliding()) return;

        if (m_eye.GetCollider() is not ItemComp itemComp) return;

        itemComp.PlayerInteract(Input.IsActionJustPressed("cat_E"), Input.IsActionJustPressed("cat_F"), this);
        GD.Print("测试:找到物品了");
    }


    /// <summary>注：信号回调 —— 当有物品进入玩家检测区域时，将其加入可交互列表。</summary>
    /// <param name="node">进入检测区域的节点。</param>
    public void DetectionAreaStart(Node node)
	{
		if (node is not ItemComp) return;

		var item = node as ItemComp;

		m_InRangeItems.Add(item);
		GD.Print($"物品[{item.Name}]进入检测区域，已加入列表，当前列表数量：{m_InRangeItems.Count}");
	}

	/// <summary>注：信号回调 —— 当有物品离开玩家检测区域时，将其从可交互列表移除。</summary>
	/// <param name="node">离开检测区域的节点。</param>
	public void DetectionAreaEnd(Node node)
	{
		if (node is not ItemComp) return;
		var item = node as ItemComp;
		if (m_InRangeItems.Contains(item))
		{
			m_InRangeItems.Remove(item);
			GD.Print($"物品[{item.Name}]离开检测区域，已从列表移除，当前列表数量：{m_InRangeItems.Count}");
		}
	}

	[Obsolete("暂时弃用")]

	/// <summary>注：每物理帧执行距离最近物品的交互检测，根据 E/F 键触发对应动作。</summary>
	/// <param name="delta">物理帧间隔（秒）。</param>
	public void UpdateInteractDetection(double delta)
	{
		if (!IsInsideTree() || m_InRangeItems.Count == 0) return;

		float minDistance = float.MaxValue;
		ItemComp closestItem = null;
		for (int i = m_InRangeItems.Count - 1; i >= 0; i--)
		{
			var item = m_InRangeItems[i];
			if (item == null || !item.IsInsideTree())
			{
				m_InRangeItems.RemoveAt(i);
				continue;
			}

			float distance = GlobalPosition.DistanceTo(item.GlobalPosition);
			if (distance < minDistance)
			{
				minDistance = distance;
				closestItem = item;
			}
		}

		if (closestItem != null)
		{
			closestItem.PlayerInteract(Input.IsActionJustPressed("cat_E"), Input.IsActionJustPressed("cat_F"), this);
			m_PickUpUI.Visible = false;
		}
	}





    /// <summary>注：处理与 UI 相关的按键输入。</summary>
    private void ProcessUIInputs()
	{
		if (Input.IsActionJustPressed("cat_Console")) m_ConsoleComp.ToggleUI();
		if (Input.IsActionJustPressed("cat_Tab"))
		{
			m_InventoryComp.RefSlot();
			m_InventoryComp.ToggleUI();
		}
		if (Input.IsActionJustPressed("cat_Esc")) m_EscComp.ToggleUI();
	}


	/// <summary>注：根据当前打开的 UI 面板自动切换鼠标模式与 UI 状态标志。</summary>
	private void UpdateMouseMode()
	{
		if (m_ConsoleComp.Visible || m_InventoryComp.Visible || m_EscComp.Visible)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
			m_OnUI = true;
		}
		else
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
			m_OnUI = false;
		}
	}

	private void InitPlayerController() => m_Controller ??= new PlayerController(this);


    private void InitPlayerAnimKeys() => m_PlayerAnimKeys ??= new PlayerAnimKeys(m_AnimationTree);
	private void InitInventory()
	{
		if (m_InventoryComp != null) return;

		var UI = UIManager.Instance.GetUI("InventoryUI");
		if (UI == null) return;
		if (UI is not InventoryComp script) return;

		script.Holder = this;
		m_InventoryComp = script;
		UI.Visible = false;
		m_CanvasLayer.AddChild(UI);
	}
	private void InitConsole()
	{
		if (m_ConsoleComp != null) return;

		var UI = UIManager.Instance.GetUI("ConsoleUI");
		if (UI == null) return;
		if (UI is not ConsoleComp script) return;

		m_ConsoleComp = script;
		m_ConsoleComp.GetPlayer(this);
		UI.Visible = false;
		m_CanvasLayer.AddChild(UI);
	}
	private void InitEsc()
	{
		if (m_EscComp != null) return;

		var UI = UIManager.Instance.GetUI("esc_ui");
		if (UI == null) return;
		if (UI is not EscComp script) return;

		m_EscComp = script;
		UI.Visible = false;
		m_CanvasLayer.AddChild(UI);
	}

	/// <summary>注：验证所有关键组件引用非空，避免后续操作因空引用崩溃。</summary>
	/// <returns>所有必要组件均存在时返回 true，否则 false。</returns>
	private bool ValidateComponents()
	{
		if (m_eye == null)
		{
			GD.PrintErr("[Player.ValidateComponents]：m_eye 字段为空");
			return false;
		}
		if (m_Camera == null)
		{
			GD.PrintErr("[Player.ValidateComponents]：m_Camera 字段为空");
			return false;
		}
		if (m_PlayerModel == null)
		{
			GD.PrintErr("[Player.ValidateComponents]：m_PlayerModel 字段为空");
			return false;
		}
		if (m_PickUpUI == null)
		{
			GD.PrintErr("[Player.ValidateComponents]：m_PickUpUI 字段为空");
			return false;
		}
		if (m_CanvasLayer == null)
		{
			GD.PrintErr("[Player.ValidateComponents]：m_CanvasLayer 字段为空");
			return false;
		}
		if (m_PlayerData == null)
		{
			GD.PrintErr("[Player.ValidateComponents]：m_PlayerData 字段为空");
			return false;
		}
		if (m_AnimationTree == null)
		{
			GD.PrintErr("[Player.ValidateComponents]：m_AnimationTree 字段为空");
			return false;
		}
		return true;
	}




	#region 测试方法

	public InventoryComp GetInventory() => m_InventoryComp;

	public CanvasLayer GetCanvasLayer() => m_CanvasLayer;

	public Vector3 GetDropPosition() => m_eye.GlobalPosition + m_eye.GlobalBasis.Z * -1.0f;

	public Godot.Collections.Dictionary<int, ItemData> LoadInventory() => m_PlayerData.m_InventoryData ?? [];

	public void SaveInventory(Array<SlotComp> slotComps) => m_PlayerData.UpdateInventoryData(slotComps);

	#endregion
}
