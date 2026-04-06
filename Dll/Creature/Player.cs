using Godot;
using System;
using System.Collections.Generic;
using 维修公司.Dll;
using 维修公司.Utils;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Creature;
using static Godot.TextServer;

public partial class Player : Humanoid
{
	[Export] public Camera3D 摄像机;
	[Export] public Node3D 玩家模型; 
	[Export] public Control 拾取UI;
	[Export] public CanvasLayer m_CanvasLayer;


	public string PlayerName;
	public float m_Speed = 5.0f;
	public float m_Jump = 4.5f;












	/// <summary>注：玩家检测返回内的物品列表 </summary>
	public List<ItemComp> m_InRangeItems = new List<ItemComp>();
	private bool isPlayerValid = false; // 完整性检测
	private bool isPlayerMenu = false; // 是否在主菜单场景
	private PlayerController m_Controller;

	public override void _Ready()
	{
		拾取UI.Visible = false;
		Init();
	}

	public override void _Process(double delta)
	{
		m_Controller.Update(delta);

		UI();
		MouseMode();

	}


	public override void _PhysicsProcess(double delta)
	{
		if (!IsInsideTree()) return;
		m_Controller.PhysicsUpdate(delta);
		UpdateInteractDetection(delta);
	}

	#region 回调函数

	/// <summary>回调函数：检测进入范围内的节点</summary>
	/// <param name="node">外部信号传入</param>
	public void DetectionAreaStart(Node node)
	{
		if (node is ItemComp item)
		{
			GD.Print("找到了一个物品");
			// 新增：将物品加入列表（仅这一行）
			if (!m_InRangeItems.Contains(item))
			{
				m_InRangeItems.Add(item);
				GD.Print($"物品[{item.Name}]进入检测区域，已加入列表，当前列表数量：{m_InRangeItems.Count}");
			}
		}
	}

	/// <summary>回调函数：检测离开范围内的节点</summary>
	/// <param name="node">外部信号传入</param>
	public void DetectionAreaEnd(Node node)
	{
		if (node is ItemComp item)
		{
			// 遍历列表找到对应物品并删除
			if (m_InRangeItems.Contains(item))
			{
				m_InRangeItems.Remove(item);
				GD.Print($"物品[{item.Name}]离开检测区域，已从列表移除，当前列表数量：{m_InRangeItems.Count}");
			}
			// 隐藏拾取UI（如果离开的是当前提示的物品）
			拾取UI.Visible = false;
		}
	}

	#endregion

	#region 组件
	/// <summary>注：背包组件 </summary>
	public InventoryComp m_InventoryComp;  
	/// <summary>注：控制台组件 </summary>
	public ConsoleComp m_ConsoleComp;   

	#endregion


	/// <summary>每帧执行的互动检测核心函数（手动放入_PhysicsProcess或_Process）</summary>
	/// <param name="delta">帧时间</param>
	public void UpdateInteractDetection(double delta)
	{
		if (!IsInsideTree() || m_InRangeItems.Count == 0) return;

		// 1. 遍历列表，找到距离玩家最近的物品
		ItemComp closestItem = null;
		float minDistance = float.MaxValue;
		foreach (var item in m_InRangeItems)
		{
			if (item == null || !item.IsInsideTree())
			{
				// 清理无效物品（比如已被销毁的）
				m_InRangeItems.Remove(item);
				continue;
			}

			// 计算物品与玩家的世界坐标距离
			float distance = GlobalPosition.DistanceTo(item.GlobalPosition);
			if (distance < minDistance)
			{
				minDistance = distance;
				closestItem = item;
			}
		}

		// 2. 显示/隐藏拾取UI（只对最近的物品显示）
		if (closestItem != null)
		{
			拾取UI.Visible = true;
		}
		else
		{
			拾取UI.Visible = false;
		}

		// 3. 检测cat_E物理按键，拾取最近的物品（核心修改：直接检测按键，不依赖IsEPressed）
		if (closestItem != null)
		{
			closestItem.PlayerInteract(Input.IsActionJustPressed("cat_E"), Input.IsActionJustPressed("cat_F"), this);
			拾取UI.Visible = false;
		}
	}



	#region  UI操作
	/// <summary>注：UI触发按钮集合 </summary>
	private void UI()
	{
		GoConsoleComp();
		GoInventoryComp();
	}

	private void GoConsoleComp()
	{
		if (Input.IsActionJustPressed("cat_Console"))
		{
			m_ConsoleComp.ToggleUI(); 
		}
	}
	private void GoInventoryComp()
	{ 
		if (Input.IsActionJustPressed("cat_Tab"))
		{
			m_InventoryComp.ToggleUI();

		}
	}

	private void MouseMode()
	{
		if (m_ConsoleComp.Visible)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		else if (!m_ConsoleComp.Visible)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}

	}

	#endregion


	#region 初始化
	/// <summary>注：player类所有初始化集合</summary>
	private void Init()
	{
		if (CheckPlayerNull())
		{
			m_Controller = new PlayerController(this);

			// 组件初始化
			InitPlayerInventory();
			InitPlayerConsole();

		}


	}
	/// <summary>注：检测player关键字段是否为空</summary>
	private bool CheckPlayerNull()
	{
		if (m_eye == null)
		{
			GD.PrintErr($"[Player.CheckPlayerNull]：检测 [m_eye] 字段为空");
			return false;
		}
		if (摄像机 == null)
		{
			GD.PrintErr($"[Player.CheckPlayerNull]：检测 [m_Camera3D] 字段为空");
			return false;
		}
		if (玩家模型 == null)
		{
			GD.PrintErr($"[Player.CheckPlayerNull]：检测 [m_PlayerMesh] 字段为空");
			return false;
		}
		if (拾取UI == null)
		{
			GD.PrintErr($"[Player.CheckPlayerNull]：检测 [拾取UI] 字段为空");
			return false;
		}
		if (m_CanvasLayer == null)
		{
			GD.PrintErr($"[Player.CheckPlayerNull]：检测 [m_CanvasLayer] 字段为空");
			return false;
		}
		isPlayerValid = true;
		return true;
	}




	/// <summary>初始化玩家背包</summary>
	private void InitPlayerInventory()
	{
		if (m_InventoryComp != null) return;

		var UI = GameCore.Instance.m_UIManager.GetUI("InventoryUI");
		if (UI == null) return;

		var script = ToolUtils.GetNodeScript<InventoryComp>(UI);
		if (script == null) return;

		script.m_Marker3D = m_eye;   // 这是初始化玩家眼睛
		m_InventoryComp = script;

		UI.Visible = false;
		m_CanvasLayer.AddChild(UI);

	}

	/// <summary>注：初始化玩家控制台</summary>
	private void InitPlayerConsole()
	{
		if (m_ConsoleComp != null) return;

		var UI = GameCore.Instance.m_UIManager.GetUI("ConsoleUI");
		if (UI == null) return;

		var script = ToolUtils.GetNodeScript<ConsoleComp>(UI);
		if (script == null) return;

		m_ConsoleComp = script;
		m_ConsoleComp.GetPlayer(this);  // 这是获取玩家组件，准备获取位置

		UI.Visible = false;
		m_CanvasLayer.AddChild(UI);

	}
	#endregion


}
