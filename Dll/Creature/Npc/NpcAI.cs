using Godot;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Data;

namespace 途畔归所.Dll.Creature.Npc
{
    public partial class NpcAI : Node
    {
        private enum NpcState
        {
            Patrol = 0,
            Chase = 1
        }

        private Npc m_owner;
        private NpcData m_data;
        private NavigationAgent3D m_nav;
        private NpcMovement m_perception;

        private NpcState m_currentState = NpcState.Patrol;
        private float m_stopTimer;
        private CreatureBase m_chaseTarget;



        public override void _Ready()
        {
            if (GetParent() is not Npc npc)
            {

                return;
            }

            m_owner = npc;
            m_data = npc.m_NpcData ?? new NpcData();

            foreach (var comp in m_owner.GetChildren())
            {
                if (comp is NavigationAgent3D nav) m_nav = nav;

                //if (comp is NpcPerception per) m_perception = per;
            }




        }








    }
}
