using Godot;
using Godot.Collections;
using 维修公司.Dll.data;
using static InventoryComp;

namespace 途畔归所.Dll.Data
{
    [GlobalClass]
    public partial class InventoryData : Resource
    {
        public enum InventoryType
        {
            Backpack = 0,
            Chest = 1,
        }

        [Export] public Array<ItemData> m_SlotDatas = [];

        public InventoryData DeepCopy() => this.DuplicateDeep() as InventoryData;
    }
}
