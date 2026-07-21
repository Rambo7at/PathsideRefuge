using Godot;
using Godot.Collections;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Utils;
using static 维修公司.Dll.data.ItemData;

namespace 途畔归所.Dll.View
{
	/// <summary>注：装备栏视图，管理主手/副手槽位的显示与交互委托绑定。</summary>
	public partial class EquipmentView : Control
	{
		[Export] private Array<SlotView> EquipSlots { get; set; } // 装备槽列表（编辑器配置）

		private IEquipmentHolder m_Holder { get; set; }            // 持有者接口
		private Dictionary<string, SlotView> EquipSlotDict = [];   // 槽位字典（按名称索引）

		public override void _EnterTree()
		{
			if (!InitHolder())
			{
				CatUtils.StopAndExit(this);
				return;
			}

			CollectSlots();
		}

		/// <summary>注：验证装备栏配置并获取持有者引用。</summary>
		private bool InitHolder()
		{
			if (EquipSlots == null || EquipSlots.Count == 0)
			{
				CatLog.Err($"[EquipmentView._EnterTree]：装备栏未有添加对应的 SlotView，已销毁");
				return false;
			}

			if (GetParent() is not IEquipmentHolder holder)
			{
				CatLog.Err($"[EquipmentView._EnterTree]：父对象没有 IEquipmentHolder 接口，已销毁");
				return false;
			}

			m_Holder = holder;
			return true;
		}

		/// <summary>注：遍历槽位，按类型存入字典并绑定委托。</summary>
		private void CollectSlots()
		{
			for (int i = 0; i < EquipSlots.Count; i++)
			{
				var slot = EquipSlots[i];
				if (slot == null)
				{
					CatLog.Warn($"[EquipmentView._EnterTree]：检查到空的 SlotView 索引{i}");
					continue;
				}

				if (!slot.IsEquipSlot)
				{
					CatLog.Warn($"[EquipmentView._EnterTree]：SlotView 未开启装备选项 索引{i}");
					continue;
				}

				switch (slot.SlotType)
				{
					case E_EquipAVL.None:
						CatLog.Warn($"[EquipmentView._EnterTree]：SlotView 的 SlotType=None 索引{i}");
						continue;
					case E_EquipAVL.MainHand:
						EquipSlotDict["MainHand"] = slot;
						continue;
					case E_EquipAVL.OffHand:
						EquipSlotDict["OffHand"] = slot;
						continue;
					case E_EquipAVL.BothHands:
					case E_EquipAVL.TwoHand:
						// 暂不支持，留空
						continue;
				}
			}

			if (!EquipSlotDict.TryGetValue("MainHand", out SlotView mainHandSlot))
			{
				CatLog.Warn($"[EquipmentView._EnterTree]：未找到 MainHand 槽位");
			}

			if (!EquipSlotDict.TryGetValue("OffHand", out SlotView offHandSlot))
			{
				CatLog.Warn($"[EquipmentView._EnterTree]：未找到 OffHand 槽位");
			}

			BindSlotEvents(mainHandSlot);
			BindSlotEvents(offHandSlot);
		}

		/// <summary>注：为指定槽位绑定数据委托（获取/设置物品数据）。</summary>
		private void BindSlotEvents(SlotView slot)
		{
			if (slot == null) return;

			slot.OnDropPos += () => m_Holder.DropPos;

			if (slot.SlotType == E_EquipAVL.MainHand)
			{
				slot.OnGetItem += () => m_Holder.Equipment.MainHandData;
				slot.OnSetItem += (newdata) => m_Holder.Equipment.MainHandData = newdata;
			}
			else if (slot.SlotType == E_EquipAVL.OffHand)
			{
				slot.OnGetItem += () => m_Holder.Equipment.OffHandData;
				slot.OnSetItem += (newdata) => m_Holder.Equipment.OffHandData = newdata;
			}
		}

		/// <summary>注：切换装备栏显示状态。</summary>
		public void ToggleUI()
		{
			Visible = !Visible;
			if (Visible) RefreshAllSlots();
		}

		/// <summary>注：刷新所有槽位的显示。</summary>
		public void RefreshAllSlots()
		{
			foreach (var slot in EquipSlots)
			{
				slot?.Refresh();
			}
		}
	}

}
