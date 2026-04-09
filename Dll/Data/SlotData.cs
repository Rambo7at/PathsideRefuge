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

        public Button m_SlotButton;       // 按钮
        public Vector2 m_StartPos;        // 按钮初始位置
        public Label m_SlotLabel;         // 文字
        public ItemData m_ItemData;       // 物品数据


        public bool IsSlotNull { get => m_ItemData == null; } 
        public bool IsDrag { get; set; } = false; // 是否正在拖拽

        public void Init(Button btn, Label label)
        {
            m_SlotButton = btn;
            m_SlotLabel = label;
            m_StartPos = btn.GlobalPosition;
            m_ItemData = new ItemData(); // 初始化空物品数据
            IsDrag = false;
        }

        public void Refresh()
        {
            if (IsSlotNull)
            {
                m_SlotLabel.Text = string.Empty;
                m_SlotButton.Icon = null;
            }
            else
            {
                m_SlotLabel.Text = $"{m_ItemData.m_Name} x{m_ItemData.m_Stack}";
                m_SlotButton.Icon = m_ItemData.m_Icon;
            }
        }
    }
}
