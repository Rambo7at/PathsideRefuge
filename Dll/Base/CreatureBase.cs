using Godot;
using Godot.Collections;
using System;
using 途畔归所.Dll.Comp;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Creature.StateMachine;
using static 途畔归所.Dll.Data.CreatureData;

namespace 途畔归所.Dll.Base
{
	public partial class CreatureBase : CharacterBody3D, IDamageable
	{
		// 导出属性
		[Export] public CreatureData m_CreatureData { get; set; }
		[Export] public Node3D m_Eye { get; set; }
		[Export] public CreatureAnimComp m_AnimComp { get; set; }

		[Export] private Array<PackedScene> m_AttackPrefabs = [];

		// 组合类
		public NetSyncBase m_NetSyncBase;
		public NetTransformSync m_NetTransformSync;
		public StateMachine m_StateMachine;
		public AnimationTree m_AnimationTree;
        public PhysicsRayQueryParameters3D m_PhysicsRay;

        // 字段属性
        public string m_Name => m_CreatureData.Name;
		public E_CreatureType m_CreatureType => m_CreatureData.CreatureType;
		public E_Faction m_Faction => m_CreatureData.Faction;

		public int m_Level => m_CreatureData.Level;
		public int m_Strength => m_CreatureData.Strength;
		public int m_Agility => m_CreatureData.Agility;
		public int m_Constitution => m_CreatureData.Constitution;
		public int Vitality => m_CreatureData.Vitality;
		public int m_Resilience => m_CreatureData.Resilience;

		public float m_Speed => m_CreatureData.Speed;
		public float m_Jump => m_CreatureData.Jump;

		public float m_Health { get => m_CreatureData.Health; set => m_CreatureData.Health = value; }
		public float m_Stamina { get => m_CreatureData.Stamina; set => m_CreatureData.Stamina = value; }
		public float m_Mana { get => m_CreatureData.Mana; set => m_CreatureData.Mana = value; }

		public float m_MaxHealth => m_CreatureData.MaxHealth;
		public float m_MaxStamina => m_CreatureData.MaxStamina;
		public float m_MaxMana => m_CreatureData.MaxMana;
		public float m_BaseDamage => m_CreatureData.BaseDamage;

		public float m_Damage => FinalDamage();

		public float m_CritChance => m_CreatureData.CritChance;
		public float m_StaggerTime => m_CreatureData.StaggerTime;

		public float m_StaggerDamage => m_CreatureData.StaggerDamage * m_MaxHealth;

		public Array<DropBase> m_DropTable => m_CreatureData.DropTable;
        public InventoryData m_InventoryData { get => m_CreatureData.InventoryData; set => m_CreatureData.InventoryData = value; }

        // 公共成员
        public Array<ItemComp> m_AttackItems = [];
        public bool m_IsOwner => m_NetSyncBase != null && m_NetSyncBase.IsOwner;
        public bool IsDead => m_Health <= 0;
		public Vector3 DropPos => GlobalPosition + Vector3.Up * 1f;



		public Rid Rid => GetRid();

        public Array<Rid> m_SelfExclude;

        /// <summary>注：受击事件</summary>
        public event Action<float, Node> OnHitEvent;

		public override void _EnterTree()
		{
			// 寻找挂载组件
			foreach (var node in GetChildren())  
			{
				m_NetSyncBase ??= node as NetSyncBase;

				m_NetTransformSync ??= node as NetTransformSync;

				m_StateMachine ??= node as StateMachine;

				m_AnimationTree ??= node as AnimationTree;
			}

			if (ValCoreComp() == false) return;

			// 初始化生命值
			m_Health = m_Health == default ? m_MaxHealth : m_Health;

            InitRegisterRpc();
			LoadAttackItems();

		}

		public override void _Ready()
		{
            if (NetCore.Instance.IsClient && m_NetSyncBase.IsInit)
            {
                m_NetSyncBase.CallRpc("RPC_RequestHealth");
            }

            m_SelfExclude ??= [Rid];  // 初始化射线 屏蔽字段
        }

		/// <summary>客户端请求主机结算伤害</summary>
		protected virtual void RPC_RequestDamage(long senderId, float amount)
		{
			if (NetCore.Instance.IsClient) return;
			// 网络伤害走统一处理层，保证校验、计算逻辑一致
			ProcessDamage(amount, null);
		}

		/// <summary>客户端接收主机广播的血量同步</summary>
		protected virtual void RPC_SyncHealth(long senderId, float newHealth)
		{
			if (NetCore.Instance.IsHost) return;
			m_Health = newHealth;
			OnDeath();
		}

		/// <summary>主机向请求者单独发送当前血量</summary>
		protected virtual void RPC_RequestHealth(long senderId)
		{
			if (NetCore.Instance.IsClient) return;
			m_NetSyncBase.CallRpc("RPC_SyncHealth", m_Health, senderId);
		}

		/// <summary>对外伤害入口（外部统一调用）</summary>
		public virtual void TakeDamage(float amount, Node node = null)
		{
			ProcessDamage(amount, node);
		}

		/// <summary>伤害处理过渡层（前置校验、效果触发、网络分流）</summary>
		protected virtual void ProcessDamage(float amount, Node attacker)
		{
			if (IsDead) return;

            OnHitEvent?.Invoke(amount, attacker);

			if (NetCore.Instance.IsHost)
			{
				m_NetSyncBase.CallAllRpc("RPC_SyncHealth", ApplyDamage(amount));
				OnDeath();
			}
			else
			{
				m_NetSyncBase.CallRpc("RPC_RequestDamage", amount);
			}
		}

		/// <summary>最终伤害结算（仅主机调用，纯数值逻辑）</summary>
		/// <returns>扣血后的剩余血量</returns>
		protected virtual float ApplyDamage(float amount)
		{
			m_Health -= amount;
			CatLog.Debug($"{m_Name}被命中 剩余血量 {m_Health}");
			return m_Health;
		}

		/// <summary>死亡钩子，子类重写以生成掉落物等</summary>
		protected virtual void OnDeath()
		{
			if (!IsDead) return;

			m_StateMachine?.SwitchAnimState(AnimState.Death);
			foreach (var drop in m_DropTable)
			{
				if (drop == null) continue;
				foreach (var item in drop.GetItemDrop())
				{
					NetObjectManager.Instance.SpawnObject(DropPos, Vector3.Zero, default, item);
				}
			}
		}

		/// <summary>注：攻击力计算</summary>
		protected virtual float FinalDamage() => m_BaseDamage;

		/// <summary> 注：重力 </summary>
		public virtual void ApplyGravity(double delta)
		{
			if (IsOnFloor()) return;
			Velocity += GetGravity() * (float)delta;
		}


		public virtual void SetPhysicsRay(Vector3 from, Vector3 to, Array<Rid> rids, uint mask = default)
		{
			m_PhysicsRay ??= new PhysicsRayQueryParameters3D();
			m_PhysicsRay.From = from;
			m_PhysicsRay.To = to;
			m_PhysicsRay.Exclude = rids;

			if (mask != default)
			{
				m_PhysicsRay.CollisionMask = mask;

            }
		}




        /// <summary> 注：智能转向，移动时面朝速度方向，静止时面朝目标点 </summary>
        public void FaceMovementOrTarget(Vector3 lookTarget, float rotationSpeed, float delta)
		{
			Vector3 horizontalVel = new Vector3(Velocity.X, 0, Velocity.Z);
			Vector3 target;

			if (horizontalVel.LengthSquared() > 0.01f)
			{
				target = GlobalPosition + horizontalVel;
				// 移动中：朝速度方向
			}
			else
			{
				target = lookTarget; // 静止：朝最终目标
			}

			Vector3 dir = target - GlobalPosition;
			dir.Y = 0;
			if (dir.LengthSquared() < 0.001f) return;

			float newY = Mathf.LerpAngle(GlobalRotation.Y, Mathf.Atan2(dir.X, dir.Z) - Mathf.Pi, rotationSpeed * delta);
			GlobalRotation = new Vector3(GlobalRotation.X, newY, GlobalRotation.Z);
		}

		/// <summary> 辅助：检查关键成员完整性 </summary>
		private bool ValCoreComp()
		{
			// 组件检查
			if (m_NetSyncBase == null || m_NetTransformSync == null || m_StateMachine == null || m_Eye == null || m_AnimComp == null || m_AnimationTree == null)
			{
				string loga = m_NetSyncBase == null ? "m_NetSyncBase/" : string.Empty;
				string logb = m_NetTransformSync == null ? "m_NetTransformSync/" : string.Empty;
				string logd = m_StateMachine == null ? "m_StateMachine/" : string.Empty;
				string logE = m_Eye == null ? "m_Eye/" : string.Empty;
				string logF = m_AnimComp == null ? "m_AnimComp/" : string.Empty;
				string logg = m_AnimationTree == null ? "m_AnimationTree/" : string.Empty;


				CatLog.Err($"[CreatureBase.ValCoreComp]: {Name}-{m_Name} 缺少核心组件：{loga + logb + logd + logE + logF + logg}，请检查编译器");
				CatUtils.StopAndExit(this);
				return false;
			}

			return true;
		}

		/// <summary> 辅助：初始化RPC </summary>
		private void InitRegisterRpc()
		{
            if (!m_NetSyncBase.IsInit) return;
            m_NetSyncBase.RegisterRpc<float>("RPC_RequestDamage", RPC_RequestDamage);
			m_NetSyncBase.RegisterRpc<float>("RPC_SyncHealth", RPC_SyncHealth);
			m_NetSyncBase.RegisterRpc("RPC_RequestHealth", RPC_RequestHealth);
		}

		/// <summary> 辅助：获取攻击列表中的PackedScene 转换为 itemComp </summary>
		private void LoadAttackItems()
		{
			if (m_AttackPrefabs.Count == 0) return;

			foreach (var item in m_AttackPrefabs)
			{
				var itemcomp = ItemManager.Instance.GetItemDrop(ItemManager.Instance.GetItemData(item).ID);
				if (itemcomp == null) continue;
				m_AttackItems.Add(itemcomp);
			}
		}
	}

}
