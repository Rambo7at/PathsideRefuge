using Godot;
using Godot.Collections;
using System;
using System.Security.AccessControl;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Data.CreatureData;

namespace 途畔归所.Dll.Base
{
	public partial class CreatureBase : CharacterBody3D, IDamageable
	{
		[Export] public CreatureData m_CreatureData { get; set; }
		[Export] public AnimationTree m_AnimationTree { get; set; }
		[Export] public RayCast3D m_Eye { get; set; }

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
		public float m_BaseAttack => m_CreatureData.BaseAttack;
		public float m_CritChance => m_CreatureData.CritChance;
		public float m_StaggerTime => m_CreatureData.StaggerTime;

		public float m_StaggerDamage => m_CreatureData.StaggerDamage * m_MaxHealth;

		public Array<DropBase> m_DropTable => m_CreatureData.DropTable;
		public Vector3 DropPos => GlobalPosition + Vector3.Up * 1f;
		public bool IsDead => m_Health <= 0;

		protected NetSyncBase m_NetSyncBase;
		protected NetTransformSync m_NetTransformSync;
		protected NetAnimationSync m_NetAnimationSync;

		/// <summary>注：受击事件</summary>
		public event Action<float, Node> OnHit;


		public override void _EnterTree()
		{
			if (m_AnimationTree == null || m_Eye == null)
			{
				CatLog.Err($"[CreatureBase._EnterTree]: {Name}-{m_Name} 缺少核心组件，请检查编译器");
				CatUtils.StopAndExit(this);
				return;
			}

			foreach (var node in GetChildren())
			{
				if (node is NetSyncBase netSync) m_NetSyncBase = netSync;
				if (node is NetTransformSync netTransform) m_NetTransformSync = netTransform;
				if (node is NetAnimationSync netAnimation) m_NetAnimationSync = netAnimation;
			}

			if (m_NetSyncBase == null || m_NetTransformSync == null || m_NetAnimationSync == null)
			{
				CatLog.Err($"[CreatureBase._EnterTree]: {Name}-{m_Name} 缺少网络组件，请检查编译器");
				CatUtils.StopAndExit(this);
				return;
			}

			m_Health = m_Health == default ? m_MaxHealth : m_Health;

			m_NetSyncBase.RegisterRpc<float>("RPC_RequestDamage", RPC_RequestDamage);
			m_NetSyncBase.RegisterRpc<float>("RPC_SyncHealth", RPC_SyncHealth);
			m_NetSyncBase.RegisterRpc("RPC_RequestHealth", RPC_RequestHealth);
		}

		public override void _Ready()
		{
			if (NetCore.Instance.IsHost)
			{
				m_NetSyncBase.CallAllRpc("RPC_SyncHealth", m_Health);
			}
			else
			{
				m_NetSyncBase.CallRpc("RPC_RequestHealth");
			}
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

			OnHit?.Invoke(amount, attacker);

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
			foreach (var drop in m_DropTable)
			{
				if (drop == null) continue;
				foreach (var item in drop.GetItemDrop())
				{
					NetObjectManager.Instance.SpawnObject(DropPos, Vector3.Zero, default, item);
				}
			}
			CatUtils.StopAndExit(this);
		}

		/// <summary> 注：重力 </summary>
		public virtual void ApplyGravity(double delta)
		{
			if (IsOnFloor()) return;
			Velocity += GetGravity() * (float)delta;
		}

		/// <summary> 注：水平移动 </summary>
		public virtual bool MoveHorizontally(Vector3 point, float speed)
		{
			Vector3 toTarget = point - GlobalPosition;
			toTarget.Y = 0;

			if (toTarget.LengthSquared() < 0.001f)
			{
				// 停止水平速度，避免惯性滑动
				Vector3 vel = Velocity;
				vel.X = 0;
				vel.Z = 0;
				Velocity = vel;
				return false;
			}

			Vector3 direction = toTarget.Normalized();
			Vector3 vel2 = Velocity;
			vel2.X = direction.X * speed;
			vel2.Z = direction.Z * speed;
			Velocity = vel2;
			return true;
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
	}

}
