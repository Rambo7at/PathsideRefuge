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

public partial class Player : Humanoid
{
	[Export] public Camera3D m_Camera;
	[Export] public Node3D m_PlayerModel;
	[Export] public CanvasLayer m_CanvasLayer;

    [Export] public StateMachine m_StateMachine;
    public PlayerUIHandler m_PlayerUIHandler;

    [Export] public BoneAttachment3D m_HandL;
    [Export] public BoneAttachment3D m_HandR;


    private bool m_IsPlayerMenu = false;
	public bool m_OnUI = false;

	public PlayerData m_PlayerData;
	private PlayerController m_Controller;
	public PlayerAnimKeys m_PlayerAnimKeys;

    [Obsolete("弃用字段")] public float m_BaseAttackDamage = 20f;
    [Obsolete("弃用字段")] private List<ItemComp> m_InRangeItems = [];

	public void Equip(ItemData itemData)
	{
		var drop = itemData.DataToDrop();
        if (drop == null ) return;

        var item = drop as ItemComp;
        m_HandR.AddChild(item);
    }


    public override void _Ready()
	{
		if (!ValidateComponents()) return;
		InitPlayerAnimKeys();
		InitPlayerUIHandler();
		InitPlayerController();

    }

	public override void _Process(double delta)
	{
		m_Controller.Update(delta);
		m_PlayerUIHandler.Updata();

    }

	public override void _PhysicsProcess(double delta)
	{
		if (!IsInsideTree()) return;
		m_Controller.PhysicsUpdate(delta);
		CheckRaycastInteract();
	}

    /// <summary>
    /// 注：执行一次攻击，使用射线检测命中的目标并造成伤害
    /// </summary>
    private void PerformAttack()
    {
        if (m_eye == null || !m_eye.IsColliding()) return;

        var target = m_eye.GetCollider();
        if (target == null) return;

        if (target is IDamageable damageable)
        {
            damageable.TakeDamage(m_BaseAttackDamage, this);
            GD.Print($"[Player] 攻击命中目标: {((Node)target).Name}");
        }
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

	/// <summary> 注：物品进入检测区域时添加到列表 </summary>
	public void DetectionAreaStart(Node node)
	{
		if (node is not ItemComp) return;

		var item = node as ItemComp;

		m_InRangeItems.Add(item);
		GD.Print($"物品[{item.Name}]进入检测区域，已加入列表，当前列表数量：{m_InRangeItems.Count}");
	}

	/// <summary> 注：物品离开检测区域时从列表移除 </summary>
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
	/// <summary> 注：更新交互检测（已弃用） </summary>
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
		}
	}

	/// <summary> 注：初始化玩家控制器 </summary>
	private void InitPlayerController() => m_Controller ??= new PlayerController(this);

	/// <summary> 注：初始化玩家UI处理器 </summary>
	private void InitPlayerUIHandler() => m_PlayerUIHandler ??= new PlayerUIHandler(this);

	/// <summary> 注：初始化玩家动画参数键 </summary>
	private void InitPlayerAnimKeys() => m_PlayerAnimKeys ??= new PlayerAnimKeys(m_AnimationTree);

    /// <summary> 注：验证所有关键组件是否非空 </summary>
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
		if (m_StateMachine == null)
		{
            GD.PrintErr("[Player.ValidateComponents]：m_StateMachine 字段为空");
            return false;

        }
		return true;
	}
}
