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

        public Dictionary<PackedScene, ItemData> m_ItemDataDict = [];

        /// <summary>注：加载资源</summary>
        /// <param name="packedScene">预制件列表</param>
        public void RegisterItem(PackedScene packedScene, ItemData item)
		{
			if (packedScene == null || item == null) return;

			if (m_ItemDataDict.ContainsKey(packedScene)) return;

			m_ItemDataDict[packedScene] = item.DeepCopy();
        }
        /// <summary>注：获取物品</summary>
        public ItemComp GetItemDrop(string itemName)
        {
            var prefab = NetObjectManager.Instance.GetPrefab(itemName);
            if (prefab?.Instantiate() is not ItemComp item)
            {
                string err = prefab == null ? "预制件不存在" : "目标预制件不是 ItemComp 类型";
                GD.PrintErr($"[GetItemDrop] {itemName} {err}");
                return null;
            }
            return item;
        }
		/// <summary>注：加载物品数据</summary>
		/// <param name="itemName">预制件名称</param>
		/// <returns>ItemData副本，失败返回null</returns>
		public ItemData GetItemData(string itemName) => GetItemData(NetObjectManager.Instance.GetPrefab(itemName));
        public ItemData GetItemData(PackedScene prefab)
        {
            if (m_ItemDataDict.TryGetValue(prefab, out var data)) return data.DeepCopy(); 


            if (prefab?.Instantiate() is not ItemComp comp)
            {
                string err = prefab == null ? "为空" : $"实例化失败，资源非ItemComp 类型 路径：{prefab?.ResourcePath}";
                GD.PrintErr($"[GetItemData] 预制件 {err} ");
                return null;
            }

            data = comp.m_ItemData.DeepCopy();
            comp.QueueFree();
            if (data != null) m_ItemDataDict[prefab] = data;

            return m_ItemDataDict[prefab].DeepCopy();
        }
    }


}
