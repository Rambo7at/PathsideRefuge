using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 维修公司.Dll.data;

namespace 途畔归所.Dll.Data
{
    public partial class SlotData : Resource
    {

        [Export] public ItemData m_ItemData;       // 物品数据
        [Export] public int m_SlotIndex;

        public bool IsSlotNull { get => m_ItemData == null; }

        public SlotData CopyData()
        {
            var data = new SlotData();
            data.m_ItemData = (m_ItemData == null) ? null : m_ItemData.CopyData();
            return data;
        }
    }
}
