using Godot;
using System;
using 维修公司.Dll;
using 维修公司.Dll.data;
using 维修公司.Dll.Interface;
using 维修公司.Dll.Manager;


/// <summary> 注：游戏场景中可拾取的物品掉落实体，包含物品基础属性和拾取逻辑</summary>
public partial class ItemComp : RigidBody3D, IInteractable
{
    public ItemData m_ItemData { get; set; }

    /// <summary>注：收纳类物品的 </summary>
    public ItemData m_boxItem { get; set; }

    [Export] public string 物品ID { get; set; }
    [Export] public string 名称 { get; set; }
    [Export] public ItemData.ItemType 类型 { get; set; }
    [Export] public string 介绍 { get; set; }
    [Export] public Texture2D 图标 { get; set; }
    [Export] public int 最大堆叠 { get; set; } = 1;
    [Export] public float 重量 { get; set; } = 1f;
    [Export] public int 体积 { get; set; } = 1;
    [Export] public int 收纳容量 { get; set; } = 1;
    public int 堆叠 { get; set; } = 1;



    public override void _Ready() => Init();

    public override void _Process(double delta)
    {
    }

    public ItemData CreateItemData() => new ItemData(this);

    #region  初始化
    public void Init()
    {
        if (m_ItemData != null) return;

        m_boxItem = ItemManager.Instance.GetItemData("手电筒");

       m_ItemData = new ItemData(this);  
    }




    #endregion
    /// <summary>互动：拾取功能 </summary>
    private void PickUp(PlayerController player)
    {
        player.m_InventoryComp.AddItem(this);
        GD.Print($"已拾取物品[{名称}]，添加到背包");
        QueueFree();
        // 拾取后从列表移除并隐藏UI
        player.m_InRangeItems.Remove(this);
    }

    /// <summary>互动：拆快递 </summary>
    private void Unbox()
    {
        if (类型 is ItemData.ItemType.收纳 && m_boxItem != null)
        {

            var drop = m_boxItem.DataToDrop();

            m_boxItem = null;

            GetTree().CurrentScene.AddChild(drop);

            drop.GlobalPosition = new Vector3(this.GlobalPosition.X, this.GlobalPosition.Y + 2, this.GlobalPosition.Z); 

        }
    }

    public void PlayerInteract(bool InputE,bool InputF,PlayerController player)
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