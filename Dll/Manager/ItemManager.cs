using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using 维修公司.Dll.data;
using 途畔归所.Dll.Manager;
using 维修公司.Utils;
using 途畔归所.Dll.Base;

namespace 维修公司.Dll
{
	/// <summary>物品资源管理器</summary>
	public class ItemManager 
	{

		public Godot.Collections.Dictionary<string, PackedScene> m_ItemDict = [];



		/// <summary>注：加载资源</summary>
		/// <param name="packedScene">预制件列表</param>
		public void Init(PackedScene packedScene)
		{
			if (packedScene == null) return;

			if (!(packedScene.Instantiate() is ItemComp)) return;
			string prefabName = ToolUtils.GetResourceName(packedScene.ResourcePath);

			if (prefabName == null) return;
			if (m_ItemDict.ContainsKey(prefabName))
			{
				GD.Print($"[ItemManager.InitItemDict]：物品 {prefabName} 已存在，跳过");
				return;
			}

			m_ItemDict.Add(prefabName, packedScene);
		}


		/// <summary>获取预制件</summary>
		/// <param name="itemName">预制件名称</param>
		/// <returns>独立的RigidBody3D实例，失败返回null</returns>
		public RigidBody3D GetItemDrop(string itemName)
		{
			if (!m_ItemDict.TryGetValue(itemName, out var prefab))
			{
				GD.PrintErr($"[GetItem] 预制件 {itemName} 不存在");
				return null;
			}

			Node itemNode = prefab.Instantiate();
			if (itemNode == null)
			{
				GD.PrintErr($"[GetItem] 预制件 {itemName} 实例化失败");
				return null;
			}

			RigidBody3D itemInstance = itemNode as RigidBody3D;
			if (itemInstance == null)
			{
				GD.PrintErr($"[GetItem] 预制件 {itemName} 根节点不是 RigidBody3D");
				itemNode.QueueFree(); 
				return null;
			}

			return itemInstance;
		}


		/// <summary>注：加载物品数据</summary>
		/// <param name="itemName">预制件名称</param>
		/// <returns>ItemData副本，失败返回null</returns>
		public ItemData GetItemData(string itemName)
		{
			if (!m_ItemDict.TryGetValue(itemName, out var prefab))
			{
				GD.PrintErr($"[GetItemData] 预制件 {itemName} 不存在");
				return null;
			}

			Node itemNode = prefab.Instantiate();
			if (itemNode == null)
			{
				GD.PrintErr($"[GetItemData] 预制件 {itemName} 实例化失败");
				return null;
			}

			ItemComp script = itemNode as ItemComp; 
			if (script == null)
			{
				GD.PrintErr($"[GetItemData] 预制件 {itemName} 不是ItemDrop类型（根节点需继承ItemDrop）");
				itemNode.QueueFree(); 
				return null;
			}

			try
			{
				ItemData itemData = script.CreateItemData();

				return itemData;
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[GetItemData] 提取 {itemName} 数据失败：{ex.Message}");
				return null;
			}
			finally
			{
				if (itemNode != null && !itemNode.IsQueuedForDeletion())
				{
					itemNode.QueueFree();
				}
			}
		}


	}


}
