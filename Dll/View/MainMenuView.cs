using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 途畔归所.Dll.View
{
	public partial class MainMenuView : Control
	{
		[Export] private Control m_PlayMenuView;

		public override void _Ready()
		{
			this.Visible = true;
		}

		/// <summary>回调函数：进入大厅菜单</summary>
		private void StartGame()
		{
			this.Visible = false;
			m_PlayMenuView.Visible = true;
		}
		/// <summary>回调函数：退出游戏</summary>
		public void QuitGame() => GetTree().Quit();






	}
}
