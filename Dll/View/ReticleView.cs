using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 途畔归所.Dll.View
{
	public partial class ReticleView : Control
	{

		float DotRadius = 1.0f;

		Color DotColor = Colors.White;




		public override void _Draw()
		{
			DrawCircle(Vector2.Zero, DotRadius, DotColor);
		}








	}
}
