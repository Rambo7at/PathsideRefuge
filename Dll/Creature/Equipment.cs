using Godot;
using Godot.Collections;
using System;
using 维修公司.Dll.data;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Comp;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using static 维修公司.Dll.data.ItemData;

namespace 途畔归所.Dll.Creature;

/// <summary>注：装备管理组件，挂载于 Humanoid 上，负责主手/副手武器的加载、切换与动画绑定。</summary>
public partial class Equipment : Node
{
	private Humanoid m_Humanoid;
	private StateMachine StateMachine { get => m_Humanoid?.m_StateMachine; }

	// 装备数据容器（索引0=主手，1=副手）
	private Dictionary<E_EquipAVL, ItemData> EquipData { get => m_Humanoid.m_EquipData; set => m_Humanoid.m_EquipData = value; }

	/// <summary>注：主手武器数据</summary>
	public ItemData MainHandData { get => EquipData[E_EquipAVL.MainHand]; set => TrySetEquipData(E_EquipAVL.MainHand, value); }

	/// <summary>注：副手武器数据</summary>
	public ItemData OffHandData { get => EquipData[E_EquipAVL.OffHand]; set => TrySetEquipData(E_EquipAVL.OffHand, value); }

	private ItemComp m_Unarmed;      // 空手（默认武器）

	public ItemComp MainHandComp;    // 当前主手武器实例
	public ItemComp OffHandComp;     // 当前副手武器实例

	public override void _EnterTree()
	{
		if (GetParent() is not Humanoid humanoid)
		{
			CatUtils.StopAndExit(this);
			CatLog.Err($"[Equipment._EnterTree]：父对象不是 Humanoid，已销毁");
			return;
		}

		m_Humanoid = humanoid;

		InitEquipData();
		LoadUnarmed();
	}

	public override void _Ready()
	{
		UpdateEquipment();
	}

	/// <summary>注：加载永久空手武器（作为默认装备）</summary>
	private void LoadUnarmed()
	{
		m_Unarmed ??= ItemManager.Instance.GetItemDrop("7at_空拳头");
		if (m_Unarmed == null)
		{
			CatLog.Warn("[Equipment.LoadUnarmed] 空拳头加载失败");
			return;
		}
	}

	public void UpdateEquipment()
	{
		foreach (var equip in Enum.GetValues(typeof(E_EquipAVL)))
		{
			UpdateEquipment((E_EquipAVL)equip);
		}
	}

	public void UpdateEquipment(E_EquipAVL equip)
	{
		switch (equip)
		{
			case E_EquipAVL.MainHand:
				UpdateMainHand(equip);
				break;
			case E_EquipAVL.OffHand:
				UpdateOffHand(equip);
				break;
		}
	}

	/// <summary>注：更新主手武器</summary>
	public void UpdateMainHand(E_EquipAVL equip)
	{
		ClearWeapon(equip,ref MainHandComp);

		// 若空手已挂载则解除，准备重新挂载
		if (m_Unarmed.IsInsideTree())
		{
			m_Unarmed.UnbindAnim(equip,m_Humanoid);
			m_Humanoid.m_HandR.RemoveChild(m_Unarmed);
		}

		if (MainHandData?.ToDrop() is not ItemComp mainHandComp)
		{
			m_Unarmed.SetEquip(equip, m_Humanoid);
			NotifyStateMachine();
			return;
		}

		MainHandComp = mainHandComp;
		MainHandComp.SetEquip(equip, m_Humanoid);
		NotifyStateMachine();
	}

	/// <summary>注：更新副手武器</summary>
	public void UpdateOffHand(E_EquipAVL equip)
	{
		ClearWeapon(equip, ref OffHandComp);

		if (OffHandData?.ToDrop() is not ItemComp offHandComp)
		{
			NotifyStateMachine();
			return;
		}

		OffHandComp = offHandComp;
		OffHandComp.SetEquip(equip, m_Humanoid);
		NotifyStateMachine();
	}

	/// <summary>注：卸除并销毁指定武器实例（解绑动画 + 释放节点）。</summary>
	private void ClearWeapon(E_EquipAVL equip,ref ItemComp weaponComp)
	{
		if (weaponComp != null)
		{
			weaponComp.UnbindAnim(equip,m_Humanoid);
			weaponComp.QueueFree();
			weaponComp = null;
		}
	}

	/// <summary>注：初始化装备数据容器</summary>
	private void InitEquipData()
	{
		if (!EquipData.TryGetValue(E_EquipAVL.MainHand, out _)) EquipData[E_EquipAVL.MainHand] = null;
		if (!EquipData.TryGetValue(E_EquipAVL.OffHand, out _)) EquipData[E_EquipAVL.OffHand] = null;
	}

	/// <summary>注：通知状态机当前装备状态</summary>
	private void NotifyStateMachine()
	{
		var mainType = MainHandData?.EquipType ?? E_EquipType.None;
		var offType = OffHandData?.EquipType ?? E_EquipType.None;

		// Stance：主手装备类型（双手斧/双手剑等驱动持械姿态）
		StateMachine.SwitchStance(mainType);

		// Defense：副手盾牌优先，否则无防御
		var defenseType = offType == E_EquipType.Shield ? E_EquipType.Shield : E_EquipType.None;
		StateMachine.SwitchDefense(defenseType);

		// 同步装备状态到状态机（供 IsMainHandEquipped / IsOffHandEquipped 查询）
		StateMachine.SwitchEquipmentState(E_EquipAVL.MainHand, mainType);
		StateMachine.SwitchEquipmentState(E_EquipAVL.OffHand, offType);
	}

	/// <summary>注：设置指定槽位的装备数据，并刷新对应槽位显示</summary>
	public bool TrySetEquipData(E_EquipAVL equip, ItemData data)
	{
		if (StateMachine.Attack) return false;

		if (!EquipData.TryGetValue(equip, out _)) EquipData[equip] = null;

		E_EquipType equipType = data == null ? E_EquipType.None : data.EquipType;

		if (equip == E_EquipAVL.MainHand && data != null && data.IsTwoHandWeapon && StateMachine.IsOffHandEquipped)
		{
			CatLog.Warn("[SetEquipData] 副手有装备，无法装上双手武器");
			return false;
		}

		if (equip == E_EquipAVL.OffHand && data != null && StateMachine.IsMainHandEquipped && StateMachine.IsMainHandTwoHand)
		{
			CatLog.Warn("[SetEquipData] 主手已装备双手武器，无法装备副手");
			return false;
		}

		EquipData[equip] = data;
		UpdateEquipment(equip);
		return true;
	}
}
