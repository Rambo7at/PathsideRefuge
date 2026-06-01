using Godot;
using Godot.Collections;
using System;
using 维修公司.Dll.data;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Manager
{
	/// <summary>注：物品资源管理器</summary>
	public class ItemManager 
	{

        private static ItemManager _instance;
        public static ItemManager Instance => _instance ??= new ItemManager();

        private ItemManager() { }

        public Dictionary<string, PackedScene> m_ItemDict = [];



		/// <summary>注：加载资源</summary>
		/// <param name="packedScene">预制件列表</param>
		public void Init()
		{
			if (ResourceManager.Instance.m_ItemAssetList == null || ResourceManager.Instance.m_ItemAssetList.Count == 0) return;

            foreach (var item in ResourceManager.Instance.m_ItemAssetList)
            {
                string prefabName = CatUtils.GetResourceName(item.ResourcePath);
                if (prefabName == null) continue;
                if (m_ItemDict.ContainsKey(prefabName))
                {
                    GD.Print($"[ItemManager.InitItemDict]：物品 {prefabName} 已存在，跳过");
                    continue;
                }

                m_ItemDict.Add(prefabName, item);

            }
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
				return script.m_ItemData;
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



        public bool HasPrefab(string prefabPath)
        {
            // 根据 m_ItemDict 的键值设计，可能需要存储完整路径，或在此处提取文件名
            // 假设 Init 时已经用完整路径或名称作为 Key，此处按需匹配
            string name = CatUtils.GetResourceName(prefabPath);
            return name != null && m_ItemDict.ContainsKey(name);
        }


    }


}
