using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;

namespace 途畔归所.Dll.Comp
{
	public partial class EscComp : UIPanelBase
	{


		private void Quit()
		{ 
		
		
		}



		private void Save() => GameCore.Instance.SaveGame();



		public void ToggleUI() => this.Visible = !this.Visible;




	}
}
