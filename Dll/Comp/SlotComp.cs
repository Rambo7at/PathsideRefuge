using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Data;

namespace 途畔归所.Dll.Comp
{
	public partial class SlotComp : UIPanelBase
	{
		[Export] public Button m_button;
		[Export] public TextureRect m_icon;
		[Export] public Label m_text;
		public SlotData SlotData { get; set; }

		public override void _Ready()
		{
			if (m_button == null) SlotData = new SlotData();
        }






	}
}
