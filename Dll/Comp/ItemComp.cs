using Godot;
using Godot.Collections;
using 维修公司.Dll.data;
using 维修公司.Dll.Interface;
using 途畔归所.Dll.Core;


/// <summary> 注：游戏场景中可拾取的物品掉落实体，包含物品基础属性和拾取逻辑</summary>
public partial class ItemComp : RigidBody3D, IInteractable
{
	
	[Export] public ItemData m_ItemData { get; set; }
	[Export] public Area3D m_WeaponHitBox { get; set; }

	public bool IsEquipped { get; set; } = false;
	private Player m_player;


	public override void _Ready()
	{
		if (m_ItemData == null) return;
		InitWeapon();
	}

	public override void _Process(double delta)
	{


	}

	private void InitWeapon()
	{
		if (m_ItemData.m_Type != ItemData.ItemType.武器) return;
		if (m_WeaponHitBox == null)
		{
			GD.PrintErr($"[ItemComp.InitWeapon]：检测{m_ItemData.m_Name}-未添加 HitBox");
			return;
		}
		if (IsEquipped) Freeze = true;

	}

	/// <summary>互动：拾取功能 </summary>
	private void PickUp(Player player)
	{
		var b = player.m_PlayerUIHandler.m_InventoryComp.TryAddItem(m_ItemData);
		GD.Print($"已拾取物品[{m_ItemData.m_Name}]，添加到背包{b}");
		QueueFree();
	}

	/// <summary>注：绑定装备的玩家类(这个方法后续将会重写，由玩家管理器进行统一) </summary>
	public void BindPlayer(Player player) => m_player = player;

	public void PlayerInteract(bool InputE,bool InputF, Player player)
	{
		if (InputE)
		{
			PickUp(player);
		}
	}

}
