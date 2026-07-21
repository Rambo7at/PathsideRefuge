using Godot;
using System;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using static 维修公司.Dll.data.ItemData;

namespace 途畔归所.Dll.Creature
{
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
		public AnimState m_AnimState { get; private set; } = AnimState.Idle;
		public NpcState m_NpcState { get; private set; } = NpcState.Patrol;
		public PlayerState m_PlayerState { get; private set; } = PlayerState.Idle;

		// 动画表达式
		public bool Walk => m_AnimState == AnimState.Walk;
		public bool Jump => m_AnimState == AnimState.Jump;
		public bool Idle => m_AnimState == AnimState.Idle;
		public bool Attack => m_AnimState == AnimState.Attack;
		public bool Stagger => m_AnimState == AnimState.Stagger;
		public bool Death => m_AnimState == AnimState.Death;
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


		// 私有字段
		private CreatureBase m_Creature;
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
			if (m_AnimState == AnimState.Death) return;
			if (m_AnimState == AnimState.Stagger) return;
			if (m_AnimState == AnimState.Attack) return;

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
			if (m_AnimState == newState) return;
			if (NetCore.Instance.IsHost)
			{
				m_AnimState = newState;
				m_NetSyncBase.CallAllRpc("RPC_SyncAnimState", (int)newState);
			}
			else
			{
				m_AnimState = newState;
				m_NetSyncBase.CallRpc("RPC_RequestAnimState", (int)newState);
			}
		}

		/// <summary>注：切换玩家状态，状态不变则不执行</summary>
		public void SwitchPlayerState(PlayerState newState)
		{
			if (m_PlayerState == newState) return;
			m_PlayerState = newState;
		}

		/// <summary>注：切换NPC状态，状态不变则不执行</summary>
		public void SwitchNpcState(NpcState newState)
		{
			if (m_NpcState == newState) return;
			m_NpcState = newState;
		}

		/// <summary>注：切换攻击动作索引</summary>
		public void SwitchAttackAnimIndex(int index)
		{
			if (AttackAnimIndex == index) return;

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
		public void SwitchDefense(E_EquipType type) => DefenseIndex = (int)type;

		/// <summary>注：请求攻击（切换攻击状态并触发 OneShot）</summary>
		public void RequestAttack()
		{
			SwitchAnimState(AnimState.Attack);
			OneShot();
		}

		/// <summary>注：请求进入/退出防御姿态（仅持盾时生效）</summary>
		public void RequestDefense(bool pressed)
		{
			if (DefenseIndex != (int)E_EquipType.Shield || pressed == false)
			{
				SetDefenseBlend(0f);
				return;
			}
			SetDefenseBlend(1f);
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

		/// <summary>注：设置防御姿态混合值（驱动 AnimationTree 的 Defense 参数）</summary>
		private void SetDefenseBlend(float v) => m_Creature.m_AnimationTree.Set("parameters/Defense/blend_amount", v);

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
			m_AnimState = (AnimState)state;
		}

		private void RPC_RequestAnimState(int state)
		{
			if (NetCore.Instance.IsClient) return;

			var newState = (AnimState)state;

			if (m_AnimState == newState) return;
			m_AnimState = newState;
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
			CatLog.Ok($"[EndCombo] 执行 EndAttack {IsCombo} {ShouldCombo}");
			if (IsCombo)
			{
				ShouldCombo = true;
			}
		}

		/// <summary>注：结束攻击</summary>
		private void EndAttack()
		{
			if (m_AnimState != AnimState.Attack) return;
			IsCombo = false;
			ShouldCombo = false;
			SwitchAnimState(Speed > 0.1f ? AnimState.Walk : AnimState.Idle);
		}

		/// <summary>注：结束眩晕</summary>
		private void EndStagger()
		{
			if (m_AnimState != AnimState.Stagger) return;
			SwitchAnimState(Speed > 0.1f ? AnimState.Walk : AnimState.Idle);
		}

		/// <summary>注：结束死亡（销毁角色）</summary>
		private void EndDeath()
		{
			if (m_AnimState != AnimState.Death) return;
			CatUtils.StopAndExit(m_Creature);
		}
		#endregion
	}
}
