using Godot;
using System;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static 维修公司.Dll.data.ItemData;

namespace 途畔归所.Dll.Creature;
/// <summary>注：角色状态机，管理动画状态、玩家/NPC行为状态、网络同步与攻击连段。</summary>
[GlobalClass]
public partial class StateMachine : Node
{
	public enum NpcState
	{
		Patrol = 0, // 巡逻
		Chase = 1,  // 追击
	}

	public enum PlayerState
	{
		Idle = 0,
		Interact = 1,
		Build = 2,
		Menu = 3
	}

	public enum AnimState
	{
		Idle = 0,
		Walk = 1,
		Run = 2,
		Jump = 3,
		Fall = 4,
		Attack = 5,
		Stagger = 6,
		Death = 7,
	}

	// 状态属性
	public AnimState CurrentAnimState { get; private set; } = AnimState.Idle;
	public NpcState CurrentNpcState { get; private set; } = NpcState.Patrol;
	public PlayerState CurrentPlayerState { get; private set; } = PlayerState.Idle;

	// 主手装备类型
	public E_EquipType MainHandType { get; private set; } = E_EquipType.None;
	// 副手装备类型
	public E_EquipType OffHandType { get; private set; } = E_EquipType.None;

	// 动画表达式
	public bool Walk => CurrentAnimState == AnimState.Walk;
	public bool Jump => CurrentAnimState == AnimState.Jump;
	public bool Idle => CurrentAnimState == AnimState.Idle;
	public bool Attack => CurrentAnimState == AnimState.Attack;
	public bool Stagger => CurrentAnimState == AnimState.Stagger;
	public bool Death => CurrentAnimState == AnimState.Death;
	public int AttackAnimIndex { get; private set; }

	/// <summary>注：当前持械姿态索引（由装备类型驱动，控制 Stance 混合参数）</summary>
	public int StanceIndex { get; set; }

	/// <summary>注：当前防御类型索引（由装备类型决定，控制 Defense 混合参数）</summary>
	public int DefenseIndex { get; set; }

	/// <summary>注：是否处于连段窗口</summary>
	public bool IsCombo { get; set; }

	/// <summary>注：是否触发连段</summary>
	public bool ShouldCombo { get; set; }

	// 便捷属性
	private bool IsOnFloor => m_Creature.IsOnFloor();
	private NetSyncBase m_NetSyncBase => m_Creature?.m_NetSyncBase;
	private float Speed => new Vector3(m_Creature.Velocity.X, 0, m_Creature.Velocity.Z).Length();
	public bool IsMainHandEquipped => MainHandType != E_EquipType.None;
	public bool IsOffHandEquipped => OffHandType != E_EquipType.None;
	public bool IsMainHandTwoHand => MainHandType == E_EquipType.TwoHandSword || MainHandType == E_EquipType.TwoHandAxe || MainHandType == E_EquipType.Staff;
	public bool IsDefending { get; private set; }

	// 私有字段
	private CreatureBase m_Creature;
	private Tween m_DefenseTween; // 防御混合动画 Tween
	private Humanoid m_Humanoid => m_Creature is Humanoid human ? human : null;
	private Player m_Player => m_Humanoid is Player pl ? pl : null;
	private Creature.Npc.Npc m_Npc => m_Humanoid is Creature.Npc.Npc npc ? npc : null;

	public override void _Ready()
	{
		if (GetParent() is not CreatureBase cr)
		{
			CatUtils.StopAndExit(this, $"[StateMachine._Ready]：检测挂载对象并非 CreatureBase ，已销毁");
			return;
		}

		m_Creature = cr;

		if (!m_Creature.m_IsOwner) SetPhysicsProcess(false);

		// 动画状态同步
		m_NetSyncBase.RegisterRpc<int>("RPC_SyncAnimState", RPC_SyncAnimState);
		m_NetSyncBase.RegisterRpc<int>("RPC_RequestAnimState", RPC_RequestAnimState);

		// OneShot 同步
		m_NetSyncBase.RegisterRpc("RPC_SyncOneShot", RPC_SyncOneShot);
		m_NetSyncBase.RegisterRpc("RPC_RequestOneShot", RPC_RequestOneShot);

		// 攻击动画索引同步
		m_NetSyncBase.RegisterRpc<int>("RPC_SyncAttackAnimIndex", RPC_SyncAttackAnimIndex);
		m_NetSyncBase.RegisterRpc<int>("RPC_RequestAttackAnimIndex", RPC_RequestAttackAnimIndex);

		// 连段标记同步
		m_NetSyncBase.RegisterRpc("RPC_SyncCombo", RPC_SyncCombo);
		m_NetSyncBase.RegisterRpc("RPC_RequestCombo", RPC_RequestCombo);

		m_Creature.m_AnimComp.OnEndStagger += EndStagger;
		m_Creature.m_AnimComp.OnEndDeath += EndDeath;
		m_Creature.m_AnimComp.OnEndAttack += EndAttack;
		m_Creature.m_AnimComp.OnEndCombo += EndCombo;
		m_Creature.m_AnimComp.OnCombo += Combo;
	}

	public override void _PhysicsProcess(double delta)
	{
		MoveState();
	}

	private void MoveState()
	{
		if (CurrentAnimState == AnimState.Death) return;
		if (CurrentAnimState == AnimState.Stagger) return;
		if (CurrentAnimState == AnimState.Attack) return;

		if (!IsOnFloor)
		{
			SwitchAnimState(m_Creature.Velocity.Y > 0 ? AnimState.Jump : AnimState.Fall);
		}
		else
		{
			SwitchAnimState(Speed > 0.1f ? AnimState.Walk : AnimState.Idle);
		}
	}

	/// <summary>注：切换动画状态，状态不变则不执行</summary>
	public void SwitchAnimState(AnimState newState)
	{
		if (CurrentAnimState == newState) return;
		if (NetCore.Instance.IsHost)
		{
			CurrentAnimState = newState;
			m_NetSyncBase.CallAllRpc("RPC_SyncAnimState", (int)newState);
		}
		else
		{
			CurrentAnimState = newState;
			m_NetSyncBase.CallRpc("RPC_RequestAnimState", (int)newState);
		}
	}

	/// <summary>注：更新装备状态（由 Equipment 调用）</summary>
	public void SwitchEquipmentState(E_EquipAVL equip, E_EquipType type)
	{
		switch (equip)
		{
			case E_EquipAVL.MainHand:
				MainHandType = type;
				break;
			case E_EquipAVL.OffHand:
				OffHandType = type;
				break;
		}
	}

	/// <summary>注：切换玩家状态，状态不变则不执行</summary>
	public void SwitchPlayerState(PlayerState newState)
	{
		if (CurrentPlayerState == newState) return;
		CurrentPlayerState = newState;
	}

	/// <summary>注：切换NPC状态，状态不变则不执行</summary>
	public void SwitchNpcState(NpcState newState)
	{
		if (CurrentNpcState == newState) return;
		CurrentNpcState = newState;
	}

	/// <summary>注：切换攻击动作索引</summary>
	public void SwitchAttackAnimIndex(int index)
	{
		if (AttackAnimIndex == index || index < 0) return;

		if (NetCore.Instance.IsHost)
		{
			AttackAnimIndex = index;
			m_NetSyncBase.CallAllRpc("RPC_SyncAttackAnimIndex", index);
		}
		else
		{
			AttackAnimIndex = index;
			m_NetSyncBase.CallRpc("RPC_RequestAttackAnimIndex", index, 1);
		}
	}

	/// <summary>注：切换持械姿态（根据装备类型驱动 Stance 混合）</summary>
	public void SwitchStance(E_EquipType type)
	{
		if (type != E_EquipType.TwoHandAxe)
		{
			SetStanceBlend(0f);
			return;
		}

		StanceIndex = (int)type;
		SetStanceBlend(1f);
	}

	/// <summary>注：设置防御类型索引（由装备类型决定）</summary>
	public void SwitchDefense(E_EquipType type)
	{
		DefenseIndex = (int)type;

		// 卸下盾牌时强制退出防御状态
		if (type != E_EquipType.Shield)
		{
			IsDefending = false;
			SetDefenseBlend(0f);
		}
	}

	/// <summary>注：请求攻击（切换攻击状态并触发 OneShot）</summary>
	public void RequestAttack()
	{
		SwitchAnimState(AnimState.Attack);
		OneShot();
	}

	/// <summary>注：请求进入/退出防御姿态（仅持盾时生效）</summary>
	public void RequestDefense(bool pressed)
	{
		if (DefenseIndex != (int)E_EquipType.Shield)
		{
			//CatLog.Ok("退出防御模式");
			IsDefending = false;
			SetDefenseBlend(0f);
			return;
		}

		if (!pressed)
		{
			//CatLog.Ok("退出防御模式");
			IsDefending = false;
			SetDefenseBlend(0f);
			return;
		}

		if (IsDefending) return;

		// 如果 Tween 还在运行，说明举盾动画尚未完成，不重复创建
		if (m_DefenseTween != null && m_DefenseTween.IsRunning())
		{
			return;
		}

		// 开始举盾动画（回调中才设置 IsDefending = true）
		SetDefenseBlend(1f, () => Test());
	}

	private void Test()
	{
		IsDefending = true;

		//CatLog.Ok("进入防御模式");

	}



	/// <summary>注：请求受击眩晕</summary>
	public void RequestStagger()
	{
		SwitchAnimState(AnimState.Stagger);
		OneShot();
	}

	/// <summary>注：请求进入连段窗口</summary>
	public void RequestCombo()
	{
		if (IsCombo) return;

		if (NetCore.Instance.IsHost)
		{
			IsCombo = true;
			m_NetSyncBase.CallAllRpc("RPC_SyncCombo");
		}
		else
		{
			IsCombo = true;
			m_NetSyncBase.CallRpc("RPC_RequestCombo");
		}
	}

	private void OneShot()
	{
		if (NetCore.Instance.IsHost)
		{
			FireOneShotLocal();
			m_NetSyncBase.CallAllRpc("RPC_SyncOneShot");
		}
		else
		{
			FireOneShotLocal();
			m_NetSyncBase.CallRpc("RPC_RequestOneShot", 1);
		}
	}

	/// <summary>注：设置持械姿势混合值（驱动 AnimationTree 的 Stance 参数）</summary>
	private void SetStanceBlend(float v) => m_Creature.m_AnimationTree.Set("parameters/Stance/blend_amount", v);

	private void SetDefenseBlend(float v, Action onComplete = null)
	{
		v = Mathf.Clamp(v, 0f, 1f);

		m_DefenseTween?.Kill();
		m_DefenseTween = CreateTween();

		m_DefenseTween.TweenProperty(m_Creature.m_AnimationTree, "parameters/Defense/blend_amount", v, 0.2f);

		if (onComplete != null)
		{
			m_DefenseTween.TweenCallback(Callable.From(onComplete));
		}
	}

	/// <summary>注：触发 OneShot（由动画状态机消费）</summary>
	private void FireOneShotLocal() => m_Creature.m_AnimationTree.Set("parameters/OneShot/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);

	#region RPC
	private void RPC_SyncOneShot()
	{
		if (NetCore.Instance.IsHost) return;
		FireOneShotLocal();
	}

	private void RPC_RequestOneShot()
	{
		if (NetCore.Instance.IsClient) return;
		FireOneShotLocal();
		m_NetSyncBase.CallAllRpc("SyncOneShot");
	}

	private void RPC_SyncAnimState(int state)
	{
		if (NetCore.Instance.IsHost) return;
		CurrentAnimState = (AnimState)state;
	}

	private void RPC_RequestAnimState(int state)
	{
		if (NetCore.Instance.IsClient) return;

		var newState = (AnimState)state;

		if (CurrentAnimState == newState) return;
		CurrentAnimState = newState;
		m_NetSyncBase.CallAllRpc("RPC_RequestAnimState", state);
	}

	/// <summary>注：客户端接收主机同步的攻击动画索引</summary>
	private void RPC_SyncAttackAnimIndex(int index)
	{
		if (NetCore.Instance.IsHost) return;
		AttackAnimIndex = index;
	}

	/// <summary>注：主机接收客户端发来的攻击索引变更请求</summary>
	private void RPC_RequestAttackAnimIndex(int index)
	{
		if (NetCore.Instance.IsClient) return;
		if (AttackAnimIndex == index) return;

		AttackAnimIndex = index;
		m_NetSyncBase.CallAllRpc("RPC_SyncAttackAnimIndex", index);
	}

	/// <summary>注：客户端接收主机同步的连段标记</summary>
	private void RPC_SyncCombo()
	{
		if (NetCore.Instance.IsHost) return;
		IsCombo = true;
	}

	/// <summary>注：主机接收客户端发来的连段请求</summary>
	private void RPC_RequestCombo()
	{
		if (NetCore.Instance.IsClient) return;
		if (IsCombo) return;

		IsCombo = true;
		m_NetSyncBase.CallAllRpc("RPC_SyncCombo");
	}
	#endregion

	#region 动画函数
	/// <summary>注：初始化连段检测</summary>
	private void Combo()
	{
		IsCombo = false;
		ShouldCombo = false;
	}

	/// <summary>注：连段窗口结束，判断是否触发连段</summary>
	private void EndCombo()
	{
		if (IsCombo)
		{
			ShouldCombo = true;
		}
	}

	/// <summary>注：结束攻击</summary>
	private void EndAttack()
	{
		if (CurrentAnimState != AnimState.Attack) return;
		IsCombo = false;
		ShouldCombo = false;
		SwitchAnimState(Speed > 0.1f ? AnimState.Walk : AnimState.Idle);
	}

	/// <summary>注：结束眩晕</summary>
	private void EndStagger()
	{
		if (CurrentAnimState != AnimState.Stagger) return;
		SwitchAnimState(Speed > 0.1f ? AnimState.Walk : AnimState.Idle);
	}

	/// <summary>注：结束死亡（销毁角色）</summary>
	private void EndDeath()
	{
		if (CurrentAnimState != AnimState.Death) return;
		CatUtils.StopAndExit(m_Creature);
	}
	#endregion
}
