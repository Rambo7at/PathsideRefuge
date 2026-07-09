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

        public Dictionary<int, PackedScene> m_ItemCompDict = [];

        public Dictionary<PackedScene, ItemData> m_ItemDataDict = [];

        /// <summary>注：注册资源</summary>
        /// <param name="packedScene">预制件列表</param>
        public void RegisterItem(int hash ,PackedScene packedScene, ItemData item)
		{
			if (packedScene == null || item == null) return;

            if (!m_ItemCompDict.ContainsKey(hash)) m_ItemCompDict[hash] = packedScene;

            if (!m_ItemDataDict.ContainsKey(packedScene)) m_ItemDataDict[packedScene] = item.DeepCopy();
        }

        /// <summary>注：使用哈希，获取ItemComp</summary>
        public ItemComp GetItemComp(int hash)
        {
            if (!m_ItemCompDict.TryGetValue(hash, out PackedScene packedScene))
            {
                GD.PrintErr($"[GetItemComp] 获取itemcomp 哈希未有目标对象 {hash}");
                return null;
            }

            if (packedScene.Instantiate() is not ItemComp item)
            {
                GD.PrintErr($"[GetItemComp] 获取的目标对象并不是 itemcomp 类型 {hash}");
                return null;
            }

            return item;
        }

        /// <summary>注：获取物品</summary>
        public ItemComp GetItemDrop(string itemName) => GetItemComp(CatUtils.GetStableHashCode(itemName));

        public PackedScene GetItemPackedScene(int hash)
        {
            if (!m_ItemCompDict.TryGetValue(hash, out PackedScene packedScene))
            {
                GD.PrintErr($"[GetItemPackedScene] 获取item PackedScene 失败 哈希未有目标对象 {hash}");
                return null;
            }

            return packedScene;
        }

        public PackedScene GetItemPackedScene(string itemName) => GetItemPackedScene(CatUtils.GetStableHashCode(itemName));

        public ItemData GetItemData(string itemName) => GetItemData(GetItemPackedScene(itemName));
        /// <summary>注：获取item 数据</summary>
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
