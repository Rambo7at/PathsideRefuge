using Godot;
using 维修公司.Dll.data;
using 维修公司.Dll.Interface;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using static 维修公司.Dll.data.ItemData;


/// <summary> 注：游戏场景中可拾取的物品掉落实体，包含物品基础属性和拾取逻辑</summary>
public partial class ItemComp : RigidBody3D, IInteractable
{

	[Export] public ItemData m_ItemData { get; set; }
	[Export] public MeshInstance3D m_ItemMesh { get; set; }
	[Export] public Area3D m_WeaponHitBox { get; set; }

	public E_ItemType m_ItemType => m_ItemData.Type;

    public override void _Ready()
	{
		if (m_ItemData == null)
		{
			CatUtils.StopAndExit(this);
			return;
		}

		InitWeapon();
	}

	public void SetupForEquip()
	{
        Freeze = true;
        SetCollisionLayerValue(1, false);
        SetCollisionMaskValue(1, false);

		if (CatUtils.FindChildNode<NetSyncBase>(this) is NetSyncBase netSync)
		{
            netSync.GetParent()?.RemoveChild(netSync);
            netSync.QueueFree();
        }
    }


    private void InitWeapon()
	{
		if (m_ItemType != E_ItemType.Weapon) return;
		if (m_WeaponHitBox == null)
		{
			GD.PrintErr($"[ItemComp.InitWeapon]：检测{m_ItemData.Name}-未添加 HitBox 已销毁");
			CatUtils.StopAndExit(this);
			return;
		}

	}

	public void PlayerInteract(bool InputE, bool InputF, Player player)
	{
		if (InputE)
		{
			PickUp(player);
		}
	}

    /// <summary>互动：拾取功能 </summary>
    private void PickUp(Player player)
    {
        var b = player.m_InventoryData.TryAddItem(m_ItemData);
        GD.Print($"已拾取物品[{m_ItemData.Name}]，添加到背包{b}");
        QueueFree();
    }

}
