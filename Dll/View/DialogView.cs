using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.View
{
	public partial class DialogView : Control
	{


		[Export] private Label m_Label;


		// 便捷属性
		private string m_Text { get => m_Label.Text; set => m_Label.Text = value; }


		public override void _Ready()
		{
			if (m_Label == null)
			{
				CatUtils.StopAndExit(this);
			}
		}


		public void SetDialogTet(string log)
		{
			m_Text = string.Empty;
			m_Text = log;
			Visible = true;
		}


	}
}
