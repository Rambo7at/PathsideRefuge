using Godot;
using 维修公司.Dll.data;
using 维修公司.Dll.Interface;
using 途畔归所.Dll.Core;


/// <summary> 注：游戏场景中可拾取的物品掉落实体，包含物品基础属性和拾取逻辑</summary>
public partial class ItemComp : RigidBody3D, IInteractable
{
	[Export] public ItemData m_ItemData { get; set; }

	/// <summary>注：收纳类物品的 </summary>
	public ItemData m_boxItem { get; set; }

	public override void _Ready() => Init();

	public override void _Process(double delta)
	{
	}


    public void Init()
    {
        if (m_ItemData != null) return;

		GD.Print("物品初始化失败");
    }


    /// <summary>互动：拾取功能 </summary>
    private void PickUp(Player player)
	{
		player.m_InventoryComp.AddItem(this);
		GD.Print($"已拾取物品[{m_ItemData.m_Name}]，添加到背包");
		QueueFree();
		// 拾取后从列表移除并隐藏UI
		player.m_InRangeItems.Remove(this);
	}

	/// <summary>互动：拆快递 </summary>
	private void Unbox()
	{
		if (m_ItemData.m_Type is ItemData.ItemType.收纳 && m_boxItem != null)
		{

			var drop = m_boxItem.DataToDrop();

			m_boxItem = null;

			GetTree().CurrentScene.AddChild(drop);

			drop.GlobalPosition = new Vector3(this.GlobalPosition.X, this.GlobalPosition.Y + 2, this.GlobalPosition.Z); 

		}
	}

	public void PlayerInteract(bool InputE,bool InputF, Player player)
	{
		if (InputE)
		{
			PickUp(player);
		}

		if (InputF)
		{
			Unbox();
		}

	}


}
