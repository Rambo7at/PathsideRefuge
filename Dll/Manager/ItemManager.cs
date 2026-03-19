using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using 维修公司.Dll.data;
using 维修公司.Dll.Manager;
using 维修公司.Utils;

namespace 维修公司.Dll
{
    /// <summary>物品资源管理器</summary>
    public partial class ItemManager 
	{
		private static Lazy<ItemManager> _instance = new Lazy<ItemManager>(() => new ItemManager());

		public static ItemManager Instance = _instance.Value;

        private ItemManager() { }


        public System.Collections.Generic.Dictionary<string, PackedScene>  m_ItemDict = new System.Collections.Generic.Dictionary<string, PackedScene>();



        /// <summary>初始化物品管理器</summary>
        /// <param name="packedScenes">预制件列表</param>
        public void InitItemManager(Array<PackedScene> packedScenes)
        {
            m_ItemDict.Clear();
            foreach (var prefab in packedScenes)
            {
                if (prefab == null)
                {
                    GD.PrintErr("[InitItemManager] 跳过空的预制件");
                    continue;
                }

                string prefabName = ToolUtils.GetResourceName(prefab.ResourcePath);

                if (prefabName == null) continue;

                if (m_ItemDict.ContainsKey(prefabName))
                {
                    GD.PrintErr($"[InitItemManager] 物品 {prefabName} 已存在，跳过");
                    continue;
                }

                m_ItemDict.Add(prefabName, prefab);
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
