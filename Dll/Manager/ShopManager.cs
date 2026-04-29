using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using 维修公司.Dll;
using 维修公司.Dll.data;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Core;

public partial class ShopManager : Node
{

	private static Lazy<ShopManager> _instance = new Lazy<ShopManager>(() => new ShopManager());

	public static ShopManager Instance = _instance.Value;

	/// <summary>注：商品数据</summary>
	public class ShopItemData()
	{
		public ItemData m_Good;

		public int m_Price;
	}

	/// <summary>注：网购商店的列表</summary>
	public List<ShopItemData> OnlineShopGoods = new List<ShopItemData>();


	private ShopManager() 
	{
		if (ItemManager.Instance.m_ItemDict == null && ItemManager.Instance.m_ItemDict.Count == 0)
		{
			GD.PrintErr($"[ShopManager-构造函数]：初始化失败，m_ItemDict 字典未初始化");
			Instance = null;
			return;
		}

		foreach (var itemID in ItemManager.Instance.m_ItemDict)
		{
			OnlineShopGoods.Add(new ShopItemData()
			{
				m_Good = ItemManager.Instance.GetItemData(itemID.Key),

				m_Price = GD.RandRange(10, 1000)
			}); 
		}
	}


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

}
