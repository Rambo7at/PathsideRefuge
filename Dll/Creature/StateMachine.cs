using Godot;
using System;
using System.Xml.Linq;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Creature.Npc;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using static Godot.WebSocketPeer;
using static 途畔归所.Dll.Creature.StateMachine;

namespace 途畔归所.Dll.Creature
{
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
		public bool IsCombo { get; set; }
		public bool GoCombo { get; set; }


		// 便捷属性
		private bool IsOnFloor => m_Creature.IsOnFloor();
		private NetSyncBase m_NetSyncBase => m_Creature?.m_NetSyncBase;
		private float Speed => new Vector3(m_Creature.Velocity.X, 0, m_Creature.Velocity.Z).Length();


		// 私有字段
		private CreatureBase m_Creature;
		private Humanoid m_Humanoid => m_Creature is Humanoid human ? human : null;
		private Player m_Player => m_Humanoid is Player pl ? pl : null;
		private Creature.Npc.Npc m_Npc => m_Humanoid is Creature.Npc.Npc npc ? npc : null;


		// 接口事件
		public event Action OnAnimStateChanged;

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


			m_Creature.OnHit += OnHit;
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

		private void OnHit(float damage, Node node) => SwitchAnimState(damage >= m_Creature.m_StaggerDamage ? AnimState.Stagger : m_AnimState);

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

		/// <summary> 注：切换动画状态，状态不变则不执行 </summary>
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
			//CatLog.Ok($"[State] Changed to: {newState}");
		}

		/// <summary> 注：切换玩家状态，状态不变则不执行 </summary>
		public void SwitchPlayerState(PlayerState newState)
		{
			if (m_PlayerState == newState) return;

			m_PlayerState = newState;
			//CatLog.Ok($"[State] Changed to: {newState}");
		}

		/// <summary> 注：切换NPC状态，状态不变则不执行 </summary>
		public void SwitchNpcState(NpcState newState)
		{
			if (m_NpcState == newState) return;

			m_NpcState = newState;
			//CatLog.Ok($"[State] Changed to: {newState}");
		}

		/// <summary> 注：切换攻击动作索引 </summary>
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

		public void RequestAttack()
		{
			SwitchAnimState(AnimState.Attack);
			OneShot();
		}

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
				// 用你新增的无参重载，默认发给主机
				m_NetSyncBase.CallRpc("RPC_RequestCombo");
			}
		}

		private void OneShot()
		{
			if (NetCore.Instance.IsHost)
			{
				// 主机：本地直接触发 + 广播所有客户端同步
				FireOneShotLocal();
				m_NetSyncBase.CallAllRpc("RPC_SyncOneShot");
			}
			else
			{
				// 客户端：本地预测触发 + 向主机发请求
				FireOneShotLocal();
				m_NetSyncBase.CallRpc("RPC_RequestOneShot", 1);
			}
		}

		/// <summary>辅助：触发 OneShot </summary>
		private void FireOneShotLocal()
		{
			m_Creature.m_AnimationTree.Set("parameters/OneShot/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);
		}


        #region RPC
        private void RPC_SyncOneShot()
		{
			// 主机不执行自己的同步消息
			if (NetCore.Instance.IsHost) return;
			FireOneShotLocal();
		}

		private void RPC_RequestOneShot()
		{
			// 客户端不处理请求
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

		/// <summary> 客户端接收：主机同步的攻击动画索引 </summary>
		private void RPC_SyncAttackAnimIndex(int index)
		{
			if (NetCore.Instance.IsHost) return;
			AttackAnimIndex = index;
		}

		/// <summary> 主机接收：客户端发来的攻击索引变更请求 </summary>
		private void RPC_RequestAttackAnimIndex(int index)
		{
			if (NetCore.Instance.IsClient) return;
			if (AttackAnimIndex == index) return;

			AttackAnimIndex = index;
			m_NetSyncBase.CallAllRpc("RPC_SyncAttackAnimIndex", index);
		}

        /// <summary> 客户端接收：主机同步的连段标记 </summary>
        private void RPC_SyncCombo()
        {
            if (NetCore.Instance.IsHost) return;
            IsCombo = true;
        }

        /// <summary> 主机接收：客户端发来的连段请求 </summary>
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
			GoCombo = false;
		}

        /// <summary>注：让动画使用表达式，跳转衍生连段</summary>
        private void EndCombo()
		{
			CatLog.Ok($"[EndCombo] 执行 EndAttack {IsCombo} {GoCombo}");
			if (IsCombo)
			{
				GoCombo = true;
			}
		}

        /// <summary>注：结束攻击</summary>
        private void EndAttack()
		{

			if (m_AnimState != AnimState.Attack) return;
			IsCombo = false;
			GoCombo = false;
			SwitchAnimState(Speed > 0.1f ? AnimState.Walk : AnimState.Idle);
		}
		/// <summary>注：结束眩晕</summary>
		private void EndStagger()
		{
			if (m_AnimState != AnimState.Stagger) return;

			SwitchAnimState(Speed > 0.1f ? AnimState.Walk : AnimState.Idle);
		}

		private void EndDeath()
		{
			if (m_AnimState != AnimState.Death) return;
			CatUtils.StopAndExit(m_Creature);
		}

		#endregion

	}
}
