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

        [Export] public Array<ItemData> m_SlotDatas = [];

        public InventoryData DeepCopy() => this.DuplicateDeep() as InventoryData;

        public byte[] Serialize()
        {
            var list = new List<byte[]>();
            foreach (var item in m_SlotDatas)
            {
                list.Add(item?.Serialize());
            }
            return JsonSerializer.SerializeToUtf8Bytes(list);
        }


        public void UpdetaSlotData(int indxe)
        {

            m_SlotDatas.Clear();

            for (int i = 0; i < indxe; i++)
            {
                m_SlotDatas.Add(null);
            }
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
    }
}
