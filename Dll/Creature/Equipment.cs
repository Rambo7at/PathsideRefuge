using Godot;
using Godot.Collections;
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
	private StateMachine m_StateMachine;

	// 装备数据容器（索引0=主手，1=副手）
	private Array<ItemData> EquipData { get => m_Humanoid.m_EquipData; set => m_Humanoid.m_EquipData = value; }

	/// <summary>注：主手武器数据</summary>
	public ItemData MainHandData { get => GetEquipData("MainHand"); set => SetEquipData("MainHand", value); }
	/// <summary>注：副手武器数据</summary>
	public ItemData OffHandData { get => GetEquipData("OffHand"); set => SetEquipData("OffHand", value); }

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
		m_StateMachine = humanoid.m_StateMachine;

		InitEquipData();
		LoadUnarmed();
	}

	public override void _Ready()
	{
		UpdateMainHand();
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

	/// <summary>注：更新主手武器</summary>
	public void UpdateMainHand()
	{
		ClearWeapon(ref MainHandComp);

		// 若空手已挂载则解除，准备重新挂载
		if (m_Unarmed.IsInsideTree())
		{
			m_Unarmed.UnbindAnim(m_Humanoid);
			m_Humanoid.m_HandR.RemoveChild(m_Unarmed);
		}

		if (MainHandData?.ToDrop() is not ItemComp mainHandComp)
		{
			EquipWeapon("MainHand", m_Unarmed);
			return;
		}

		MainHandComp = mainHandComp;
		EquipWeapon("MainHand", MainHandComp);
	}

	/// <summary>注：更新副手武器</summary>
	public void UpdateOffHand()
	{
		ClearWeapon(ref OffHandComp);

		if (OffHandData?.ToDrop() is not ItemComp offHandComp)
		{
			// 无副手数据 → 清空副手（不显示任何东西）
			return;
		}

		OffHandComp = offHandComp;
		EquipWeapon("OffHand", OffHandComp);
	}

	/// <summary>注：卸除并销毁指定武器实例（解绑动画 + 释放节点）。</summary>
	private void ClearWeapon(ref ItemComp weaponComp)
	{
		if (weaponComp != null)
		{
			weaponComp.UnbindAnim(m_Humanoid);
			weaponComp.QueueFree();
			weaponComp = null;
		}
	}

	/// <summary>注：将武器实例装备到指定槽位（挂载、旋转、动画绑定与状态更新）。</summary>
	private void EquipWeapon(string equipName, ItemComp weapon)
	{

		weapon.SetEquip(equipName, m_Humanoid);

	}

	/// <summary>注：初始化装备数据容器</summary>
	private void InitEquipData()
	{
		EquipData ??= [];

		while (EquipData.Count < 2)
		{
			EquipData.Add(null);
		}

		while (EquipData.Count > 2)
		{
			EquipData.RemoveAt(EquipData.Count - 1);
		}
	}

	/// <summary>注：获取指定槽位的装备数据</summary>
	private ItemData GetEquipData(string equip)
	{
		InitEquipData();

		if (equip == "MainHand") return EquipData[0];
		if (equip == "OffHand") return EquipData[1];

		return null;
	}

	/// <summary>注：设置指定槽位的装备数据，并刷新对应槽位显示</summary>
	private void SetEquipData(string equip, ItemData data)
	{
		InitEquipData();

		if (equip == "MainHand")
		{
			if (data == null || data.CanEquipMainHand)
			{
				EquipData[0] = data;
				UpdateMainHand();
			}
			return;
		}

		if (equip == "OffHand")
		{
			if (data == null || data.CanEquipOffHand)
			{
				EquipData[1] = data;
				UpdateOffHand();
			}
			return;
		}
	}
}
