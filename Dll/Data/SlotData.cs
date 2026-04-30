using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 维修公司.Dll.data;
using 途畔归所.Dll.Comp;

namespace 途畔归所.Dll.Data
{
    public partial class SlotData : Resource
    {

        [Export] public ItemData m_ItemData;       // 物品数据
        [Export] public int m_SlotIndex;

        public bool IsSlotNull { get => m_ItemData == null; }

        private SlotComp CreateSlotComp()
        {
            if (IsSlotNull)
            {
                return new SlotComp();
            }
            else
            { 
               return new SlotComp() { m_SlotData = this.DeepCopy() };
            }
        }

        public SlotData DeepCopy()
        {
            var data = this.DuplicateDeep() as SlotData;
            if (data == null) return null;

            return data;
        }
    }
}
