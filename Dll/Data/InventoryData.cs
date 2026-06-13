using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Text.Json;
using 维修公司.Dll.data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Data
{
    [GlobalClass]
    public partial class InventoryData : Resource, ISerializable
    {
        public enum InventoryType
        {
            Backpack = 0,
            Chest = 1,
        }

        [Export] public string m_UIname = "InventoryUI";

        [Export] public string m_SlotUIName = "slot_ui";

        [Export] public int m_maxCol = 1;

        [Export] public int m_maxRow = 10;
        

        [Export] public Array<ItemData> m_itemArr = [];

        public int m_capacity => m_maxCol * m_maxRow;

        public event Action OnChanged;

        /// <summary>注：尝试添加物品到库存，若成功则刷新显示，物品为空时打印错误。</summary>
        public bool TryAddItem(ItemData itemData)
        {
            if (itemData == null)
            {
                CatLog.Warn("[InventoryData.TryAddItem] 传入的ItemData为空");
                return false;
            }

            if (TryStackInventory(itemData))
            {
                OnChanged?.Invoke();  // 加 ?. 防止空引用
                return true;
            }

            int idx = FindEmptySlot();
            if (idx == -1) return false;

            m_itemArr[idx] = itemData.DeepCopy();
            OnChanged?.Invoke();
            return true;
        }


        /// <summary>注：查询库存中的空格子。</summary>
        /// <returns>空的格子组件。</returns>
        private int FindEmptySlot()
        {
            for (int i = 0; i < m_itemArr.Count; i++)
            {
                if (m_itemArr[i] == null) return i;
            }
            return -1;
        }

        /// <summary>注：查找库存中可堆叠指定物品的格子，并尝试堆叠物品，返回是否成功堆叠完物品。</summary>
        private bool TryStackInventory(ItemData itemData)
        {
            foreach (var slotdata in m_itemArr)
            {
                if (itemData.m_Stack <= 0) return true;
                if (slotdata != null && slotdata.m_ID == itemData.m_ID && slotdata.m_IsStackable) slotdata.TryStack(itemData);
            }
            return itemData.m_Stack < 1;
        }



        public byte[] Serialize()
        {
            var list = new List<byte[]>();
            foreach (var item in m_itemArr)
            {
                list.Add(item?.Serialize());
            }
            return JsonSerializer.SerializeToUtf8Bytes(list);
        }

        public void Deserialize(byte[] data)
        {
            m_itemArr.Clear();

            var dto = JsonSerializer.Deserialize<List<byte[]>>(data);

            if (dto == null) return;


            foreach (var slotdata in dto)
            {
                if (slotdata == null)
                {
                    m_itemArr.Add(null);
                    continue;
                }
                else
                {
                    var itemdata = new ItemData();
                    itemdata.Deserialize(slotdata);
                    m_itemArr.Add(itemdata);
                }
            }
        }

        public InventoryData DeepCopy() => this.DuplicateDeep() as InventoryData;

    }
}
