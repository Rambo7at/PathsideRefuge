using Godot;
using System.Xml.Linq;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Creature.StateMachine;


namespace 途畔归所.Dll.Creature.Npc
{

    public partial class Npc : Humanoid
    {
        public float m_PatrolRadius => m_CreatureData.PatrolRadius;                 // 巡逻半径
        public float m_PatrolStopTime => m_CreatureData.PatrolStopTime;             // 巡逻点停留时间
        public float m_ChaseTargetDistance => m_CreatureData.ChaseTargetDistance;     // 追击时与目标保持的距离
        public float m_RotationSpeed => m_CreatureData.RotationSpeed;              // 转身速度

        public override void _Ready()
        {
            base._Ready();


        }
    }

}
