using Godot;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Data;


namespace 途畔归所.Dll.Creature.Npc
{

	public partial class Npc : Humanoid
	{

		[Export] public NpcData m_NpcData;

		public override void _Ready()
		{
			m_NpcData ??= new NpcData();

		}
	}

}
