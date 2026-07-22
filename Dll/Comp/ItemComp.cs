using Godot;
using Godot.Collections;
using 维修公司.Dll.data;
using 维修公司.Dll.Interface;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using static 维修公司.Dll.data.ItemData;
namespace 途畔归所.Dll.Comp;


/// <summary>注：游戏场景中可拾取的物品掉落实体，包含物品基础属性和拾取逻辑</summary>
[GlobalClass]
public partial class ItemComp : RigidBody3D, IInteractable
{
	[Export] public ItemData Data { get; set; }          // 物品数据
	[Export] public Area3D WeaponHitBox { get; set; }    // 武器攻击判定盒

	public string ObjectName => Data.Name;               // 接口属性：显示名称

	private Array<Node3D> LastHitTargetArr = [];        // 上次命中的目标（防止重复命中）
	private Node3D m_Owner;                              // 持有者

	/// <summary>注：初始化物品，校验数据完整性。</summary>
	public override void _Ready()
	{
		if (Data == null)
		{
			CatUtils.StopAndExit(this);
			return;
		}

		if (Data.IsWeapon && WeaponHitBox == null)
		{
			GD.PrintErr($"[ItemComp._Ready]：{Data.Name} 未添加 HitBox，已销毁");
			CatUtils.StopAndExit(this);
			return;
		}
	}


	/// <summary>注：将物品切换为装备状态（冻结物理、禁用碰撞、移除网络组件）。</summary>
	public void SetEquip(E_EquipAVL equip, Humanoid human)
	{
		if (human == null) return;

		Freeze = true;

		if (CatUtils.FindChildNode<NetSyncBase>(this) is NetSyncBase netSync)
		{
			netSync.GetParent()?.RemoveChild(netSync);
			netSync.QueueFree();
		}

		if (equip == E_EquipAVL.OffHand && Data.EquipAVL == E_EquipAVL.BothHands)
		{
			RotateY(Mathf.Pi);
		}

		switch (equip)
		{
			case E_EquipAVL.MainHand:
				human.m_HandR.AddChild(this);
				break;
			case E_EquipAVL.OffHand:
				human.m_HandL.AddChild(this);
				break;
		}

		BindAnim(equip, human);

		// ===== 双持检测（优雅版）=====
		if (equip == E_EquipAVL.MainHand && human.m_StateMachine.IsOffHandEquipped && human.m_StateMachine.OffHandType == Data.EquipType)
		{
			human.m_StateMachine.SwitchAttackAnimIndex(Data.DualWieldIndex);
		}
		else if (equip == E_EquipAVL.OffHand && human.m_StateMachine.IsMainHandEquipped && human.m_StateMachine.MainHandType == Data.EquipType)
		{
			human.m_StateMachine.SwitchAttackAnimIndex(Data.DualWieldIndex);
		}
		else
		{
			human.m_StateMachine.SwitchAttackAnimIndex(Data.AttackAnimIndex);
		}

	}





	/// <summary>注：获取武器攻击判定盒。</summary>
	public Area3D GetHitBox() => WeaponHitBox;

	// IInteractable 接口实现（无注释）
	public void PlayerInteract(bool InputE, bool InputF, CreatureBase Creature)
	{
		if (InputE)
		{
			PickUp(Creature);
		}
	}

	/// <summary>注：拾取物品到玩家背包。</summary>
	private void PickUp(CreatureBase Creature)
	{
		bool success = Creature.m_InventoryData.TryAddItem(Data);
		GD.Print($"已拾取物品[{Data.Name}]，添加到背包：{success}");
		QueueFree();
	}

	/// <summary>注：绑定角色动画事件（攻击时启用/禁用碰撞盒）。</summary>
	public void BindAnim(E_EquipAVL equip,CreatureBase creature)
	{
		if (equip == E_EquipAVL.MainHand)
		{
			creature.m_AnimComp.OnMainHandHitEnable += EnableHitbox;
			creature.m_AnimComp.OnMainHandHitDisable += DisableHitbox;
		}
		else if (equip == E_EquipAVL.OffHand)
		{
			creature.m_AnimComp.OnOffHandHitEnable += EnableHitbox;
			creature.m_AnimComp.OnOffHandHitDisable += DisableHitbox;
		}
		m_Owner = creature;
	}

	/// <summary>注：解绑角色动画事件。</summary>
	public void UnbindAnim(E_EquipAVL equip,CreatureBase creature)
	{
		if (equip == E_EquipAVL.MainHand)
		{
			creature.m_AnimComp.OnMainHandHitEnable -= EnableHitbox;
			creature.m_AnimComp.OnMainHandHitDisable -= DisableHitbox;
		}
		else if (equip == E_EquipAVL.OffHand)
		{
			creature.m_AnimComp.OnOffHandHitEnable -= EnableHitbox;
			creature.m_AnimComp.OnOffHandHitDisable -= DisableHitbox;
		}
		m_Owner = null;
	}


	/// <summary>注：开启攻击判定窗口。</summary>
	public void EnableHitbox()
	{
		if (WeaponHitBox == null) return;

		LastHitTargetArr.Clear();
		WeaponHitBox.Monitoring = true;
		WeaponHitBox.BodyEntered += OnHit;
	}

	/// <summary>注：关闭攻击判定窗口。</summary>
	public void DisableHitbox()
	{
		if (WeaponHitBox == null) return;

		WeaponHitBox.BodyEntered -= OnHit;
		WeaponHitBox.Monitoring = false;
	}

	/// <summary>注：攻击命中回调，对目标造成伤害。</summary>
	private void OnHit(Node3D body)
	{
		if (body is not IDamageable damageable || body == m_Owner || LastHitTargetArr.Contains(body) || body == this) return;

		if (damageable is not CreatureBase creature)
		{
			ApplyDamageAndRecord(damageable, body);
			return;
		}

		if (!IsAttackFromFront(m_Owner as CreatureBase, creature))
		{
			ApplyDamageAndRecord(damageable, body);
			return;
		}

		if (!creature.m_StateMachine.IsDefending)
		{
			ApplyDamageAndRecord(damageable, body);
			return;
		}

		// 双手武器 → 无法格挡，直接扣血
		if (creature.m_StateMachine.IsMainHandTwoHand)
		{
			ApplyDamageAndRecord(damageable, body);
			return;
		}


		// 副手有盾牌 → 盾牌格挡
		if (creature.m_StateMachine.IsOffHandEquipped && creature.m_StateMachine.OffHandType == E_EquipType.Shield)
		{
			// 盾牌格挡逻辑（待实现）
			ApplyDamageAndRecord(damageable, body);
			CatLog.Ok("实现格挡！");
			return;
		}

		// 主手有武器且不是双手 → 主手武器格挡
		if (creature.m_StateMachine.IsMainHandEquipped && !creature.m_StateMachine.IsMainHandTwoHand)
		{
			// 主手武器格挡逻辑（待实现）
			ApplyDamageAndRecord(damageable, body);
			return;
		}

		// 无格挡装备 → 直接扣血
		ApplyDamageAndRecord(damageable, body);
	}

	/// <summary>注：应用伤害并记录命中目标。</summary>
	private void ApplyDamageAndRecord(IDamageable target, Node3D body)
	{
		target.TakeDamage(Data.Damage);
		LastHitTargetArr.Add(body);
		CatLog.Ok($"[ItemComp.OnHit] 命中 {body.Name}");
	}
	private bool IsAttackFromFront(CreatureBase attacker, CreatureBase target)
	{
		if (attacker == null) return false;

		// 获取目标的前方向向量（通常为 -GlobalBasis.Z）
		Vector3 targetForward = -target.GlobalBasis.Z;

		// 攻击者指向目标的方向向量
		Vector3 attackDir = (attacker.GlobalPosition - target.GlobalPosition).Normalized();

		// 计算点积：如果攻击方向和目标前方向夹角小于 90°，说明攻击来自正面
		float dot = targetForward.Dot(attackDir);

		// dot > 0 表示攻击来自正面（夹角 < 90°）
		// dot > 0.5 表示攻击来自正面 60° 范围内（略严格）
		return dot > 0.5f;
	}


}
