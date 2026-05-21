using Godot;
using 维修公司.Dll.data;
using 维修公司.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.View;

public partial class ContainerComp : PlacedComp, IInteractable
{
	[Export] public InventoryComp m_InventoryComp;
	public InventoryView m_inventoryView;

	public bool m_IsOpen { get; private set; }   // 改用独立字段

	private CanvasLayer m_CanvasLayer;

	public override void _Ready()
	{
		InitEntityBase();
		InitInventory();
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

	private void InitInventory()
	{
		if (m_InventoryComp == null)
		{

			return;
		}

		var UI = UIManager.Instance.GetUI("ContainerUI");
		if (UI is not InventoryView view) return;
		m_inventoryView = view;                  
		view.BindData(m_InventoryComp);
		view.Visible = false;
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
}
