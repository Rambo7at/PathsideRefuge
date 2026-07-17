using Godot;
using 维修公司.Dll.data;
using 维修公司.Dll.Interface;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Comp;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using static 维修公司.Dll.data.ItemData;

/// <summary> 注：游戏场景中可拾取的物品掉落实体，包含物品基础属性和拾取逻辑</summary>
[GlobalClass]
public partial class ItemComp : RigidBody3D, IInteractable
{

	[Export] public ItemData m_ItemData { get; set; }
	[Export] public Area3D m_WeaponHitBox { get; set; }

	// 便捷属性
	public int m_AttackAnimIndex => m_ItemData.AttackAnimIndex;
	public bool m_IsTwoHandedWeapon => m_ItemData.TwoHandedWeapon;
	public float m_AttackDistance => m_ItemData.AttackDistance;
    public E_ItemType m_ItemType => m_ItemData.Type;

	public string ObjectName => m_ItemData.Name;

    private Node3D m_LastHitTarget;

	private Node3D m_Owner;



    public override void _Ready()
	{
		if (m_ItemData == null)
		{
			CatUtils.StopAndExit(this);
			return;
		}

		if (m_ItemType == E_ItemType.Weapon && m_WeaponHitBox == null)
		{
			GD.PrintErr($"[ItemComp.InitWeapon]：检测{m_ItemData.Name}-未添加 HitBox 已销毁");
			CatUtils.StopAndExit(this);
			return;
		}
	}

	public void SetEquip()
	{
		Freeze = true;
		SetCollisionLayerValue(1, false);
		SetCollisionMaskValue(1, false);

		if (CatUtils.FindChildNode<NetSyncBase>(this) is NetSyncBase netSync)
		{
			netSync.GetParent()?.RemoveChild(netSync);
			netSync.QueueFree();
		}
	}

	public Area3D GetHitBox() => m_WeaponHitBox;


	/// <summary>注：玩家互动接口 </summary>
	public void PlayerInteract(bool InputE, bool InputF, CreatureBase Creature)
	{
		if (InputE)
		{
			PickUp(Creature);
		}
	}

	/// <summary>互动：拾取功能 </summary>
	private void PickUp(CreatureBase Creature)
	{
		var b = Creature.m_InventoryData.TryAddItem(m_ItemData);
		GD.Print($"已拾取物品[{m_ItemData.Name}]，添加到背包{b}");
		QueueFree();
	}


	public void BindAnim(CreatureBase creature)
	{
        creature.m_AnimComp.OnEnableHitbox += EnableHitbox;
        creature.m_AnimComp.OnDisableHitbox += DisableHitbox;
        creature.m_StateMachine.SwitchTwoHandedIdle(m_IsTwoHandedWeapon);
        m_Owner = creature;

    }

	public void UnbindAnim(CreatureBase creature)
	{
        creature.m_AnimComp.OnEnableHitbox -= EnableHitbox;
        creature.m_AnimComp.OnDisableHitbox -= DisableHitbox;
        creature.m_StateMachine.SwitchTwoHandedIdle(false);
        m_Owner = null;
    }

	// 动画轨道调用：开启判定窗口
	public void EnableHitbox()
	{
		if (m_WeaponHitBox == null) return;
		m_LastHitTarget = null;
		m_WeaponHitBox.Monitoring = true;
		m_WeaponHitBox.BodyEntered += OnHit;
	}

	// 动画轨道调用：关闭判定窗口
	public void DisableHitbox()
	{
		if (m_WeaponHitBox == null) return;
		m_WeaponHitBox.BodyEntered -= OnHit;
		m_WeaponHitBox.Monitoring = false;
	}

	/// <summary>注 Area3D回调函数 </summary>
	private void OnHit(Node3D body)
	{
		if (body is not IDamageable node || body == m_Owner || body == m_LastHitTarget) return;
		node.TakeDamage(m_ItemData.Damage);
		m_LastHitTarget = body;
		CatLog.Ok($"[PlayerAttack] 命中 {body.Name}");
	}


}
