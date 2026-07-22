using Godot;
using System.Collections.Generic;
using 维修公司.Dll.data;
using 途畔归所.Dll.Comp;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Creature.StateMachine;

namespace 途畔归所.Dll.Creature.Npc;

/// <summary>注：NPC战斗逻辑组件，管理攻击冷却、武器切换与攻击行为。</summary>
public partial class NpcBattle : Node
{
    private Npc m_Npc;
    private Equipment Equipment => m_Npc?.m_Equipment;            // 装备组件
    private StateMachine StateMachine => m_Npc?.m_StateMachine;   // 状态机

    private List<AttackInfo> m_AttackInfoList = [];

    private AnimState AnimState => StateMachine.CurrentAnimState;      // 当前动画状态
    private bool IsAttackState => AnimState == AnimState.Attack;  // 是否处于攻击状态


    /// <summary>注：攻击信息，管理单个武器的冷却状态。</summary>
    public class AttackInfo
    {
        public ItemComp Item { get; }                        // 武器实例
        public bool IsReady => CooldownRemaining <= 0;       // 冷却是否完成
        public double CooldownRemaining { get; private set; } = 0; // 剩余冷却时间

        public AttackInfo(ItemComp comp) => Item = comp;

        public void StartCooldown() => CooldownRemaining = Item.Data.AttackInterval;
        public void UpdateCooldown(double delta) => CooldownRemaining -= delta;
    }


    public override void _Ready()
    {
        if (GetParent() is not Npc node)
        {
            CatUtils.StopAndExit(this);
            return;
        }
        m_Npc = node;

        if (m_Npc.m_AttackItems.Count == 0) return;

        foreach (var item in m_Npc.m_AttackItems)
        {
            if (item?.Data.Type != ItemData.E_ItemType.Equip) continue;
            m_AttackInfoList.Add(new AttackInfo(item));
        }

        if (m_AttackInfoList.Count == 0)
        {
            CatLog.Debug("[NpcBattle._Ready]：NPC 攻击列表中没有攻击武器");
            return;
        }

        Equipment.MainHandData = m_AttackInfoList[0].Item.Data;
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateAllCooldowns(delta);
    }


    /// <summary>注：执行攻击，装备武器并触发攻击动画。</summary>
    public void TryAttack()
    {
        if (IsAttackState || m_Npc.IsDead) return;

        var info = GetReadyAttackInfo();
        if (info == null) return;

        Equipment.MainHandData = info.Item.Data;
        StateMachine.RequestAttack();
        info.StartCooldown();
    }


    /// <summary>注：获取第一个冷却完成的攻击信息。</summary>
    private AttackInfo GetReadyAttackInfo()
    {
        foreach (var info in m_AttackInfoList)
        {
            if (info.IsReady) return info;
        }
        return null;
    }


    /// <summary>注：更新所有武器的冷却计时。</summary>
    private void UpdateAllCooldowns(double delta)
    {
        foreach (var info in m_AttackInfoList)
        {
            if (info.IsReady) continue;
            info.UpdateCooldown(delta);
        }
    }
}

