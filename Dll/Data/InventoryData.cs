using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Text.Json;
using 维修公司.Dll.data;
using 途畔归所.Dll.Interface;
using static InventoryComp;

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


        [Export] public Array<ItemData> m_SlotDatas = [];

        public int GetCapacity() => m_maxCol * m_maxRow;



        public byte[] Serialize()
        {
            var list = new List<byte[]>();
            foreach (var item in m_SlotDatas)
            {
                list.Add(item?.Serialize());
            }
            return JsonSerializer.SerializeToUtf8Bytes(list);
        }

        public void Deserialize(byte[] data)
        {
            m_SlotDatas.Clear();

            var dto = JsonSerializer.Deserialize<List<byte[]>>(data);

            if (dto == null) return;


            foreach (var slotdata in dto)
            {
                if (slotdata == null)
                {
                    m_SlotDatas.Add(null);
                    continue;
                }
                else
                {
                    var itemdata = new ItemData();
                    itemdata.Deserialize(slotdata);
                    m_SlotDatas.Add(itemdata);
                }
            }
        }

        public InventoryData DeepCopy() => this.DuplicateDeep() as InventoryData;

    }
}
