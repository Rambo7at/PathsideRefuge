using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Comp;

namespace 途畔归所.Dll.Data
{

    public partial class PlayerData : Resource
    {

        [Export] public string m_Name;

        [Export] public int m_playerID;

        [Export] public Array<SlotData> m_InventoryData = new Array<SlotData>();

        public int SetPlayerID() => m_playerID = (m_playerID == default) ? Math.Abs(Guid.NewGuid().GetHashCode()) : m_playerID;

        public void UpdateInventoryData(Array<SlotData> slotDatas)
        {
            m_InventoryData.Clear();
            foreach (var data in slotDatas) m_InventoryData.Add(data.CopyData());
        }

        public void UpdateInventoryData(Array<SlotComp> slotComps)
        {
            m_InventoryData.Clear();
            foreach (var slot in slotComps) m_InventoryData.Add(slot.m_SlotData.CopyData());
        }

    }
}
