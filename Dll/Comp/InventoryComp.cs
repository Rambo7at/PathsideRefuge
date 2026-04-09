using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using 维修公司.Dll;
using 维修公司.Dll.data;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Data;


/// <summary>注：背包组件 </summary>
public partial class InventoryComp : UIPanelBase
{
	/// <summary>注：背包格子UI容器 </summary>
	[Export] private Array<PanelContainer> m_SlotUI = new Array<PanelContainer>();
	/// <summary>注：背包格子数据 </summary>
	public List<SlotData> m_inventorySlots = new List<SlotData>();

	/// <summary>注：正在拖拽的物品按钮 </summary>
	private Button m_DragBtn;
	/// <summary>注： 按下时鼠标全局位置 </summary>
	private Vector2 m_StartMousePos;
	/// <summary>注：按下时按钮全局位置 </summary>
	private Vector2 m_StartBtnPos;
	/// <summary>注：玩家丢弃物品的位置</summary>
	public Marker3D m_Marker3D;
	public override void _Ready() => Init();

	// ===================== 新增：拖拽跟随逻辑 =====================
	public override void _Process(double delta)
	{
		if (m_DragBtn != null) // 只有拖拽中才更新按钮位置
		{
			m_DragBtn.GlobalPosition = m_StartBtnPos + (GetGlobalMousePosition() - m_StartMousePos);
		}
	}
	// ======================================================================

	private void Init()
	{
		if (m_SlotUI == null || m_SlotUI.Count == 0)
		{
			GD.PrintErr("[InventoryComp.Init] 场景中未添加任何背包格子UI");
			return;
		}

		if (m_Marker3D == null)
		{
			GD.PrintErr("[InventoryComp.Init] 未增加玩家眼睛初始化失败");
			return;
		}

		m_inventorySlots.Clear();

		for (int i = 0; i < m_SlotUI.Count; i++)
		{
			if (m_SlotUI[i] == null)
			{
				GD.PrintErr($"[InventoryComp.Init] 检测到空的格子UI容器，编号-[{i}] 跳过");
				continue;
			}
			Button button = m_SlotUI[i].GetNodeOrNull<Button>("VBoxContainer/物品格子");
			Label text = m_SlotUI[i].GetNodeOrNull<Label>("VBoxContainer/Label");

			if (button == null || text == null)
			{
				GD.PrintErr($"[InventoryComp.Init] 格子[{i}]未找到物品 按钮 或 文字 ，跳过");
				continue;
			}

			// 接收鼠标事件
			button.MouseFilter = MouseFilterEnum.Stop;

			// 绑定Button原生信号（button_down/button_up）
			button.ButtonDown += () => OnBtnDown(button);
			button.ButtonUp += () => OnBtnUp(button);
			
			var DATA = new SlotData();
            DATA.Init(button, text);
            m_inventorySlots.Add(DATA);
		}
	}

	#region 回调函数
	/// <summary>触发信号：button_down</summary>
	private void OnBtnDown(Button btn)
	{
		// 核心修改：通过SlotData判断是否有物品
		SlotData sourceSlot = FindSlotDataByButton(btn);
		if (sourceSlot == null || sourceSlot.m_ItemData == null || btn.Icon == null) return;

		// 记录拖拽起始状态
		m_DragBtn = btn;
		m_StartMousePos = GetGlobalMousePosition();
		m_StartBtnPos = btn.GlobalPosition;
		btn.ZIndex = 100; // 拖拽时按钮置顶，避免被遮挡
	}

	/// <summary>触发信号：button_up</summary>
	private void OnBtnUp(Button btn) => DragSwap(btn);
	#endregion

	/// <summary>注：拖拽交换函数，使用 button_up 回调中</summary>
	private void DragSwap(Button btn)
	{
		if (m_DragBtn != btn) return;
		// 1. 先恢复按钮位置和层级
		btn.GlobalPosition = m_StartBtnPos;
		btn.ZIndex = 0;

		// 2. 新建目标按钮变量，并检测鼠标松开时落在哪个按钮区域
		Button targetBtn = null;
		Vector2 mouseGlobalPos = GetGlobalMousePosition(); // 获取松开时的鼠标全局位置

		// 遍历所有背包格子按钮，判断鼠标是否在按钮范围内
		foreach (var slotUI in m_SlotUI)
		{
			if (slotUI == null) continue;
			// 找到当前格子的物品按钮（和你Init里的路径一致）
			Button tempBtn = slotUI.GetNodeOrNull<Button>("VBoxContainer/物品格子");
			if (tempBtn == null) continue;

			// 关键判断：鼠标位置是否在按钮的全局矩形范围内
			if (tempBtn.GetGlobalRect().HasPoint(mouseGlobalPos))
			{
				targetBtn = tempBtn;
				break; // 找到目标按钮，退出循环
			}
		}

		// 3. 如果找到目标按钮（且不是自己），就执行交换
		if (targetBtn != null && targetBtn != btn)
		{
			// 核心修改：查找SlotData而非直接查找ItemData
			SlotData slotA = FindSlotDataByButton(btn);
			SlotData slotB = FindSlotDataByButton(targetBtn);

			// 校验：两个格子都有效，且物品数据不为空
			if (slotA != null && slotB != null && slotA.m_ItemData != null && slotB.m_ItemData != null)
			{
				ItemData tempItemData = slotA.m_ItemData;
				slotA.m_ItemData = slotB.m_ItemData;
				slotB.m_ItemData = tempItemData;

				RefSlot();
			}
		}
		// ===================== 新增 else 分支（丢弃逻辑）=====================
		else if (targetBtn == null) // 没找到目标按钮 = 鼠标在背包格子外松开
		{
			// 找到当前拖拽的格子数据
			SlotData dropSlot = FindSlotDataByButton(btn);
			if (dropSlot != null)
			{
				// 执行丢弃物品方法（你之前适配好的DropItem）
				DropItem(dropSlot);
			}
		}
		// ======================================================================

		// 4. 清空拖拽状态
		m_DragBtn = null;
	}


	/// <summary>注：丢弃物品到玩家Marker3D前1米处</summary>
	/// <param name="slotData">要丢弃的格子数据</param>
	private void DropItem(SlotData slotData)
	{
		// 1. 基础校验：格子/物品/Marker3D 必须有效
		if (slotData == null || slotData.m_ItemData == null || m_Marker3D == null)
		{
			GD.PrintErr("[DropItem] 丢弃失败：格子/物品/Marker3D为空");
			return;
		}

		// 2. 校验物品数据有效性
		ItemData dropItemData = slotData.m_ItemData;
		if (string.IsNullOrEmpty(dropItemData.m_ID) || dropItemData.m_Stack <= 0)
		{
			GD.PrintErr("[DropItem] 丢弃失败：物品数据为空（ID/数量无效）");
			return;
		}

		// 3. 核心：调用你提供的工具方法生成掉落物（RigidBody3D）
		RigidBody3D dropRigidBody = dropItemData.DataToDrop();
		if (dropRigidBody == null)
		{
			GD.PrintErr($"[DropItem] 丢弃失败：ItemManager未找到ID为[{dropItemData.m_ID}]的物品");
			return;
		}

		// 4. 计算丢弃位置：Marker3D前1米（沿Z轴正方向）
		Vector3 dropPosition = m_Marker3D.GlobalPosition + m_Marker3D.GlobalBasis.Z * 0.5f;

		GetTree().CurrentScene.AddChild(dropRigidBody);
		// 第二步：再设置全局位置（此时有效）
		dropRigidBody.GlobalPosition = dropPosition;

		// 7. 清空背包格子数据 + 刷新UI
		slotData.m_ItemData = new ItemData();
		RefSlot();                            // 刷新背包显示

		GD.Print($"[DropItem] 成功丢弃物品[{dropItemData.m_Name}] x{dropItemData.m_Stack} 到位置：{dropPosition}");
	}

	/// <summary>注：根据按钮查找对应的SlotData</summary>
	/// <param name="btn">物品按钮</param>
	/// <returns>SlotData</returns>
	private SlotData FindSlotDataByButton(Button btn)
	{
		if (btn == null) return null;
		return m_inventorySlots.Find(slot => slot != null && slot.m_SlotButton == btn);
	}



    #region 行为函数

    /// <summary>注：加入背包物品</summary>
    /// <param name="itemDrop">物品实体</param>
    /// <returns>布尔</returns>
    public bool AddItem(ItemComp itemDrop)
    {
        if (itemDrop == null)
        {
            GD.PrintErr("[InventoryComp.AddItem] 传入的ItemDrop节点为空");
            return false;
        }

        ItemData newItemData = itemDrop.CreateItemData();
        if (newItemData == null)
        {
            GD.PrintErr($"[InventoryComp.AddItem] 物品[{itemDrop.名称}]创建ItemData失败");
            return false;
        }

        foreach (var slotData in m_inventorySlots)
        {
            if (slotData.m_ItemData.m_ID == newItemData.m_ID && slotData.m_ItemData.IsStack())
            {
				slotData.m_ItemData.TryStack(newItemData);
            }
        }

		RefSlot();


        if (newItemData.m_Stack <= 0) return true;
	    var slot =	FindEmptySlot();
        RefSlot();
        if (slot == null) return false;
        slot.m_ItemData = newItemData;
        RefSlot();
        return true;
    }

    /// <summary>注：刷新背包格子显示</summary>
    public void RefSlot()
	{
		foreach (var slotData in m_inventorySlots) slotData.Refresh();
    }

    #endregion

    #region 辅助方法
    /// <summary>注：查询相同物品进行堆叠</summary>
    /// <param name="itemID">预制名</param>
    /// <returns>SlotData（包含可堆叠的ItemData）</returns>
    private SlotData FindStackSlot(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return null;

        foreach (var slotData in m_inventorySlots)
        {
            if (slotData == null || slotData.m_ItemData == null) continue;

            if (!string.IsNullOrEmpty(slotData.m_ItemData.m_Name) && slotData.m_ItemData.m_ID == itemID && slotData.m_ItemData.m_Stack < slotData.m_ItemData.m_MaxStack)
            {
                return slotData;
            }
        }
        return null;
    }

    /// <summary>注：查询背包中的空格子</summary>
    /// <returns>SlotData（包含空的ItemData）</returns>
    private SlotData FindEmptySlot()
    {
        foreach (var slotData in m_inventorySlots)
        {
            if (slotData == null || slotData.m_ItemData == null) continue;

            if (string.IsNullOrEmpty(slotData.m_ItemData.m_Name) && string.IsNullOrEmpty(slotData.m_ItemData.m_ID))
            {
                return slotData;
            }
        }
        return null;
    }

    /// <summary>注：检测背包是否还有空位</summary>
	/// <returns></returns>
	private bool IsInventoryEmpty()
	{
        foreach (var slotData in m_inventorySlots) if (slotData.IsSlotNull) return true;
		return false;
    }




    public void ToggleUI() => this.Visible = !this.Visible;

    #endregion

}
