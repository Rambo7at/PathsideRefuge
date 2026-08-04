using Godot;
using Godot.Collections;
using System;
using 维修公司.Dll.data;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Comp;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using 途畔归所.Dll.View;
using static 维修公司.Dll.data.ItemData;

namespace 途畔归所.Dll.Creature;

/// <summary>注：装备管理组件，挂载于 Humanoid 上，负责主手/副手武器的加载、切换与动画绑定。</summary>
public partial class Equipment : Node, IEquipmentHolder 
{
	private Humanoid m_Humanoid;

	private NetSyncBase NetSyncBase => m_Humanoid?.m_NetSyncBase;

	private StateMachine StateMachine { get => m_Humanoid?.m_StateMachine; }

	// 装备数据容器（索引0=主手，1=副手）
	private Dictionary<E_EquipAVL, ItemData> EquipData { get => m_Humanoid.m_EquipData; set => m_Humanoid.m_EquipData = value; }

	/// <summary>注：主手武器数据</summary>
	public ItemData MainHandData { get => EquipData[E_EquipAVL.MainHand]; set => TrySetEquipData(E_EquipAVL.MainHand, value); }

	/// <summary>注：副手武器数据</summary>
	public ItemData OffHandData { get => EquipData[E_EquipAVL.OffHand]; set => TrySetEquipData(E_EquipAVL.OffHand, value); }
	Equipment IEquipmentHolder.Equipment{ get => this; set { } }

	public Vector3 DropPos => m_Humanoid.DropPos;

	private ItemComp m_Unarmed;      // 空手（默认武器）

	public ItemComp MainHandComp;    // 当前主手武器实例
	public ItemComp OffHandComp;     // 当前副手武器实例

	public EquipmentView EquipmentView;

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
		if (m_Humanoid is Player) InitPlayerEquipView();


		// RPC 还有一个小BUG没处理，在客户端进入游戏时，如果镜像体本身有武器没有申请同步
		if (NetSyncBase != null)
		{
			NetSyncBase.RegisterRpc<int, string>("RPC_RequestChangeEquip", RPC_RequestChangeEquip);
			NetSyncBase.RegisterRpc<int, string>("RPC_SyncEquip", RPC_SyncEquip);
		}


		UpdateEquipment();
	}

	/// <summary>注：初始化装备数据容器</summary>
	private void InitEquipData()
	{
		if (!EquipData.TryGetValue(E_EquipAVL.MainHand, out _)) EquipData[E_EquipAVL.MainHand] = null;

		if (!EquipData.TryGetValue(E_EquipAVL.OffHand, out _)) EquipData[E_EquipAVL.OffHand] = null;
	}

	private void InitPlayerEquipView()
	{
		EquipmentView ??= GUIManager.Instance.GetView("EquipmentView") is EquipmentView view ? view : null;
		if (EquipmentView == null)
		{
			CatLog.Warn("[PlayerGUI.InitPlayerEquip] 装备栏视图加载失败");
			return;
		}

		EquipmentView.m_Holder = this;
		EquipmentView.Visible = false;
	}

	/// <summary>注：加载默认空手武器（作为默认装备）</summary>
	private void LoadUnarmed()
	{
		m_Unarmed ??= ItemManager.Instance.GetItemDrop("7at_空拳头");
		if (m_Unarmed == null)
		{
			CatLog.Warn("[Equipment.LoadUnarmed] 空拳头加载失败");
			return;
		}
	}

	/// <summary>注：设置指定槽位的装备数据，并刷新对应槽位显示</summary>
	public bool TrySetEquipData(E_EquipAVL equip, ItemData data,bool syncData = true)
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
		if (syncData)
		{
			if (NetCore.Instance.IsHost)
			{
				NetSyncBase?.CallAllRpc("RPC_SyncEquip", (int)equip, data?.ID ?? "");
			}
			else if (NetCore.Instance.IsClient)
			{
				NetSyncBase?.CallRpc("RPC_RequestChangeEquip", (int)equip, data?.ID ?? "");
			}
		}
		return true;
	}

	/// <summary>注：客户端请求更换装备（发给主机）</summary>
	private void RPC_RequestChangeEquip(long senderId, int equipSlot, string itemId)
	{
		if (NetCore.Instance.IsClient) return; // 仅主机处理

		var data = string.IsNullOrEmpty(itemId) ? null : ItemManager.Instance.GetItemData(itemId);

		TrySetEquipData((E_EquipAVL)equipSlot, data, false);
		NetSyncBase?.CallAllRpc("RPC_SyncEquip", equipSlot, itemId);
	}

	/// <summary>注：主机广播装备状态给所有客户端</summary>
	private void RPC_SyncEquip(long senderId, int equipSlot, string itemId)
	{
		if (NetCore.Instance.IsHost) return; // 客户端接收

		if (!EquipData.TryGetValue((E_EquipAVL)equipSlot, out var itemData)) return;

		if (itemData?.ID == itemId) return;

		var data = string.IsNullOrEmpty(itemId) ? null : ItemManager.Instance.GetItemData(itemId);

		// 客户端应用装备数据（fromNetwork = true 不触发 RPC）
		TrySetEquipData((E_EquipAVL)equipSlot, data, false);
	}

	/// <summary>注：更新装备栏 </summary>
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






}
