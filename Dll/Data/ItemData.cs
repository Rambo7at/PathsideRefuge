using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 维修公司.Utils;
using 途畔归所.Dll.Core;
using static ItemComp;

namespace 维修公司.Dll.data
{
    public class ItemData
    {

        public enum ItemType
        {
            消耗品,
            工具,
            收纳
        }


        public string m_ID { get; set; }
        public string m_Name { get; set; } = string.Empty;
        public ItemType m_Type { get; set; }
        public string m_Description { get; set; }
        public Texture2D m_Icon { get; set; }
        public int m_Stack { get; set; } = 1;
        public int m_MaxStack { get; set; } = 1;
        public float m_Weight { get; set; } = 1f;
        public int m_Volume { get; set; } = 1;
        public int m_Capacity { get; set; } = 1;
        public int m_MaxCapacity { get; set; } = 1;
        public ItemData(ItemComp itemDrop)
        {
            m_ID = itemDrop.物品ID;
            m_Name = itemDrop.名称;
            m_Type = itemDrop.类型;
            m_Description = itemDrop.介绍;
            m_Icon = itemDrop.图标;
            m_Stack = itemDrop.堆叠;
            m_MaxStack = itemDrop.最大堆叠;
            m_Weight = itemDrop.重量;
            m_Volume = itemDrop.体积;

        }



        public ItemData() { }


        public ItemData CopyData()
        {

            return new ItemData()
            {
                m_ID = this.m_ID,
                m_Name = this.m_Name,
                m_Type = this.m_Type,
                m_Description = this.m_Description,
                m_Icon = this.m_Icon,
                m_Stack = this.m_Stack,
                m_MaxStack = this.m_MaxStack,
                m_Weight = this.m_Weight
            };
        }

        public void CopyDataTo(ItemData itemData)
        {
            if (itemData == null) return;

            itemData.m_ID = this.m_ID;
            itemData.m_Name = this.m_Name;
            itemData.m_Type = this.m_Type;
            itemData.m_Icon = this.m_Icon;
            itemData.m_Stack = this.m_Stack;
            itemData.m_MaxStack = this.m_MaxStack;
            itemData.m_Weight = this.m_Weight;
            itemData.m_Description = this.m_Description;
        }

        public RigidBody3D DataToDrop()
        { 
            var drop = GameCore.Instance.m_ItemManager.GetItemDrop(this.m_ID);
            ToolUtils.GetNodeScript<ItemComp>(drop).m_ItemData = this;
            return drop;
        }

        public bool IsStack() => m_Stack < m_MaxStack;

        public int GetStackNum() => Mathf.Max(0, m_MaxStack - m_Stack);

        public bool TryStack(ItemData outData)
        {
            if (outData == null) return false;
            if (outData.m_ID != m_ID) return false;
            if (!IsStack()) return false;

            while (IsStack() && outData.m_Stack > 0)
            {
                m_Stack++;
                outData.m_Stack--;
            }

            return outData.m_Stack <= 0;
        }

    }




}

