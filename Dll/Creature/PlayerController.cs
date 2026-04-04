using Godot;
using System;
using System.Collections.Generic;
using 维修公司.Dll;
using 维修公司.Utils;
using 途畔归所.Dll.Core;
using static Godot.TextServer;

public partial class PlayerController : CharacterBody3D
{
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;
	/// <summary>目标旋转角度（GDScript的target_angle，初始值为PI）</summary>
	private float targetAngle = Mathf.Pi;

	/// <summary>注：玩家检测返回内的物品列表 </summary>
	public List<ItemComp> m_InRangeItems = new List<ItemComp>();

	/// <summary>玩家挂载的摄像头 </summary>
	[Export] private Camera3D m_Camera3D;
	/// <summary> 玩家镜头控制身体旋转组件，传入的是玩家模型 </summary>
	[Export] private Node3D m_PlayerMesh; // 对应你GDScript里的model变量
	/// <summary> 提示UI </summary>
	[Export] private Control 拾取UI;
	/// <summary> 玩家身上的CanvasLayer </summary>
	[Export] private CanvasLayer m_CanvasLayer;
	/// <summary> 射线组件 </summary>
	[Export] private Marker3D m_eye;


	#region 组件

	private BuildComp m_buildSystem;   // 建造系统？这个可能需要更改
	public InventoryComp m_InventoryComp;   //背包组件
	public ConsoleComp m_ConsoleComp;    //控制台组件
	public PhoneComp m_PhoneComp;   //手机组件

	#endregion





	public override void _Ready()
	{
		拾取UI.Visible = false;
		Init();
	}

	public override void _Process(double delta)
	{
		PlayerMoveAnimationDirection(delta);
		UI();
		MouseMode();
	}

	public override void _PhysicsProcess(double delta)
	{
		
		if (!IsInsideTree()) return;

		UpdateInteractDetection(delta);
		HandlePlayerMovement(delta);
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
	private void UI()
	{
		GoConsoleComp();
		GoInventoryComp();
		GoPhoneComp();
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

	private void GoPhoneComp()
	{
		if (Input.IsActionPressed("cat_B"))
		{
			m_PhoneComp.ToggleUI();
		}
	
	}

	private void MouseMode()
	{
		if (m_ConsoleComp.Visible || m_PhoneComp.Visible || m_InventoryComp.Visible)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		else if (!m_ConsoleComp.Visible && !m_PhoneComp.Visible && !m_InventoryComp.Visible)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
   
	}


	#endregion


	#region 初始化
	/// <summary>注：玩家的所有初始化集合</summary>
	private void Init()
	{
		InitPlayerInventory();
		InitPlayerConsole();
		InitPhoneComp();
	}
	/// <summary>初始化背包组件</summary>
	private void InitPlayerInventory()
	{
		if (m_InventoryComp != null) return;

		var UI = GameCore.Instance.m_UIManager.GetUI("InventoryUI");
		if (UI == null) return;

		var script = ToolUtils.GetNodeScript<InventoryComp>(UI);
		if (script == null) return;

		script.m_Marker3D = m_eye;
		m_InventoryComp = script;

		UI.Visible = false;
		m_CanvasLayer.AddChild(UI);

	}

	/// <summary>注：初始化控制台组件</summary>
	private void InitPlayerConsole()
	{
		if (m_ConsoleComp != null) return;

		var UI = GameCore.Instance.m_UIManager.GetUI("ConsoleUI");
		if (UI == null) return;

		var script = ToolUtils.GetNodeScript<ConsoleComp>(UI);
		if (script == null) return;

		m_ConsoleComp = script;
		m_ConsoleComp.GetPlayer(this);

		UI.Visible = false;
		m_CanvasLayer.AddChild(UI);

	}
	/// <summary>注：初始化手机组件</summary>
	private void InitPhoneComp()
	{

		if (m_PhoneComp != null) return;

		var UI = GameCore.Instance.m_UIManager.GetUI("PhoneUI");
		if (UI == null) return;

		var script = ToolUtils.GetNodeScript<PhoneComp>(UI);
		if (script == null) return;

		m_PhoneComp = script;

		UI.Visible = false;
		m_CanvasLayer.AddChild(UI);
	}

	private void InitBuildSystem()
	{
		// 1. 创建BuildSystem实例
		m_buildSystem = new BuildComp();
		// 2. 关键：把BuildSystem添加到Player节点下（进入场景树）
		AddChild(m_buildSystem);
		GD.PrintErr("红色提示！开始建造咯！");
		m_buildSystem.InitBuildPiece(m_eye, m_Camera3D, "测试方块");
		GD.PrintErr("建造执行成功");
	}
	#endregion


	#region 人物操作
	/// <summary>注：人物移动逻辑</summary>
	/// <param name="delta">帧时间</param>
	private void HandlePlayerMovement(double delta)
	{
		Vector3 velocity = Velocity;

		// 重力
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// 跳跃
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// 获取输入方向
		Vector2 inputDir = Input.GetVector("cat_Left", "cat_Right", "cat_Forward", "cat_Backward");

		// 计算摄像头的水平前方向和右方向（忽略俯仰）
		Vector3 cameraForward = -m_Camera3D.GlobalTransform.Basis.Z;
		Vector3 cameraRight = m_Camera3D.GlobalTransform.Basis.X;
		cameraForward.Y = 0;
		cameraRight.Y = 0;
		cameraForward = cameraForward.Normalized();
		cameraRight = cameraRight.Normalized();

		// 移动方向 = 前/后 * 摄像头前方 + 左/右 * 摄像头右方
		// 注意：inputDir.Y 向前为负，所以需要取反
		Vector3 direction = cameraForward * (-inputDir.Y) + cameraRight * inputDir.X;
		direction = direction.Normalized();

		// 设置水平速度
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}


	/// <summary>注：封装人物移动随摄像机转向 </summary>
	/// <param name="delta"></param>
	private void PlayerMoveAnimationDirection(double delta)
	{
		// 获取相机的水平旋转角度
		float cameraAngle = m_Camera3D.GlobalRotation.Y;
		// 获取输入方向
		Vector2 inputDir = Input.GetVector("cat_Left", "cat_Right", "cat_Forward", "cat_Backward");
		var inputangle = Mathf.Atan2(inputDir.X, inputDir.Y);

		// 如果存在有效输入，更新目标角度
		if (inputDir != Vector2.Zero)
		{
			targetAngle = cameraAngle + inputangle;
		}

		// 平滑旋转模型（只绕 Y 轴旋转）
		float rotationSpeed = 15f; // 对应 GDScript 中的 15
		float smoothedY = Mathf.LerpAngle(m_PlayerMesh.GlobalRotation.Y, targetAngle, (float)delta * rotationSpeed);
		m_PlayerMesh.GlobalRotation = new Vector3(m_PlayerMesh.GlobalRotation.X, smoothedY, m_PlayerMesh.GlobalRotation.Z);
	}
	#endregion

}
