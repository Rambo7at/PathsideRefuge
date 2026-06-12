using Godot;
using System.Xml.Linq;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Data;


namespace 途畔归所.Dll.Creature.Npc
{

	public partial class Npc : Humanoid
	{

		[Export] public CreatureData m_data;

		public override void _Ready() => m_data ??= new CreatureData();
    }

}
