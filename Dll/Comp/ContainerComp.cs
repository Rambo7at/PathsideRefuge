using Godot;
using 维修公司.Dll.Interface;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using 途畔归所.Dll.View;

public partial class ContainerComp : PlacedComp, IInteractable, IInventoryHolder
{
	private InventoryComp m_InventoryComp;
	private InventoryData m_inventoryData;
	public InventoryView m_inventoryView;
	public bool m_IsOpen { get; private set; }
	InventoryData IInventoryHolder.m_HolderInventoryData { get => m_inventoryData ??= new(); set => m_inventoryData = value; }

	private CanvasLayer m_CanvasLayer;

	public override void _Ready()
	{

		NetObject netObject = null;

		foreach (var node in GetChildren())
		{
			if (node is NetSyncBase comp)
			{
				if (comp.m_NetObj == null) return;

				comp.OnFlushNetState += () => FlushInventory(comp.m_NetObj);

				netObject = comp.m_NetObj;
			}

			if (node is InventoryComp inventoryComp) m_InventoryComp = inventoryComp;
		}

		if (m_InventoryComp == null)
		{
			CatUtils.StopAndExit(this);
			return;
		}

		if (netObject != null)
		{
			var custdata = netObject.m_customData.As<PlacedData>();
			m_placedData = custdata != null ? custdata.DeepCopy() : m_placedData;
		}

		var data = m_placedData.m_data.As<InventoryData>();

		if (data != null && data.m_SlotDatas != null && data.m_SlotDatas.Count > 0)
		{
			m_inventoryData = data.DeepCopy();
		}

		var UI = UIManager.Instance.GetUI("ContainerUI");
		if (UI is not InventoryView view) return;
		m_inventoryView = view;
		view.BindData(m_InventoryComp);
		view.Visible = false;
	}

	public override void _Process(double delta)
	{
		if (m_IsOpen && m_CanvasLayer != null)
		{
			Node owner = m_CanvasLayer.GetOwner();
			if (owner is Node3D node)
			{
				float distance = GlobalPosition.DistanceTo(node.GlobalPosition);
				if (distance >= 3)
				{
					// 自动关闭
					m_inventoryView?.GetParent()?.RemoveChild(m_inventoryView);
					m_IsOpen = false;
				}
			}
		}
	}

	public void OpenContainer(Player player)
	{
		if (player == null || m_inventoryView == null) return;

		if (m_IsOpen)
		{
			m_inventoryView.GetParent()?.RemoveChild(m_inventoryView);
			m_IsOpen = false;
		}
		else
		{
			player.m_CanvasLayer.AddChild(m_inventoryView);
			m_CanvasLayer = player.m_CanvasLayer;
			m_inventoryView.Visible = true;
			m_IsOpen = true;
		}
	}

	public void PlayerInteract(bool InputE, bool InputF, Player player)
	{
		if (InputE)
		{
			OpenContainer(player);
		}
	}


	private void FlushInventory(NetObject netObject)
	{
		m_placedData.m_data = m_inventoryData.DeepCopy();
		netObject.m_customData = m_placedData.DeepCopy();
	}
}
