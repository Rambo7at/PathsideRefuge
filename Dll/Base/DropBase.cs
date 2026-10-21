using Godot;
using Godot.Collections;
using 维修公司.Dll.data;
using 途畔归所.Dll.Comp;
using 途畔归所.Dll.Manager;

namespace 途畔归所.Dll.Base
{
    [GlobalClass]
    public partial class DropBase : Resource
    {
        [Export] private PackedScene m_prefab;
        [Export] public int m_minAmount = 1;
        [Export] public int m_maxAmount = 1;
        [Export] public float m_chance = 1f;
        public ItemData m_itemData => ItemManager.Instance.GetItemData(m_prefab);

        public Array<ItemComp> GetItemDrop()
        {
            Array<ItemComp> items = [];

            if (m_itemData == null || GD.Randf() > m_chance) return items;


            int count = Mathf.Max(1, GD.RandRange(m_minAmount, m_maxAmount));

            for (int i = 0; i < count; i++)
            {
                items.Add(m_itemData.ToDrop());
            }

            return items;
        }

    }
}
