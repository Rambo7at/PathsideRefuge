using Godot;
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

    private Node3D m_LastHitTarget;                      // 上次命中的目标（防止重复命中）
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
    public void SetEquip(string equipSlotName, Humanoid human)
    {

        if (human == null) return;

        Freeze = true;
        SetCollisionLayerValue(1, false);
        SetCollisionMaskValue(1, false);

        if (CatUtils.FindChildNode<NetSyncBase>(this) is NetSyncBase netSync)
        {
            netSync.GetParent()?.RemoveChild(netSync);
            netSync.QueueFree();
        }
        //// 分割线

        if (equipSlotName == "MainHand")
        {
            human.m_HandR.AddChild(this);
        }
        else if (equipSlotName == "OffHand")
        {
            human.m_HandL.AddChild(this);

            if (Data.EquipAVL == E_EquipAVL.BothHands)
            {
                RotateY(Mathf.Pi);
            }
        }


        if (Data.IsWeapon || Data.EquipType == E_EquipType.Shield)
        {
            BindAnim(human);
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
    public void BindAnim(CreatureBase creature)
    {
        creature.m_AnimComp.OnEnableHitbox += EnableHitbox;
        creature.m_AnimComp.OnDisableHitbox += DisableHitbox;
        creature.m_StateMachine.SwitchStance(Data.EquipType);
        creature.m_StateMachine.SwitchDefense(Data.EquipType);
        m_Owner = creature;
    }

    /// <summary>注：解绑角色动画事件。</summary>
    public void UnbindAnim(CreatureBase creature)
    {
        creature.m_AnimComp.OnEnableHitbox -= EnableHitbox;
        creature.m_AnimComp.OnDisableHitbox -= DisableHitbox;
        creature.m_StateMachine.SwitchStance(E_EquipType.None);
        creature.m_StateMachine.SwitchDefense(E_EquipType.None);
        m_Owner = null;
    }

    /// <summary>注：开启攻击判定窗口（由动画轨道调用）。</summary>
    public void EnableHitbox()
    {
        if (WeaponHitBox == null) return;
        m_LastHitTarget = null;
        WeaponHitBox.Monitoring = true;
        WeaponHitBox.BodyEntered += OnHit;
    }

    /// <summary>注：关闭攻击判定窗口（由动画轨道调用）。</summary>
    public void DisableHitbox()
    {
        if (WeaponHitBox == null) return;
        WeaponHitBox.BodyEntered -= OnHit;
        WeaponHitBox.Monitoring = false;
    }

    /// <summary>注：攻击命中回调，对目标造成伤害。</summary>
    private void OnHit(Node3D body)
    {
        if (body is not IDamageable node || body == m_Owner || body == m_LastHitTarget) return;
        node.TakeDamage(Data.Damage);
        m_LastHitTarget = body;
        CatLog.Ok($"[ItemComp.OnHit] 命中 {body.Name}");
    }
}

