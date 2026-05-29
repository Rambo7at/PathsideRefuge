using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 途畔归所.Dll.Data
{

    [GlobalClass]
    public partial class NpcData : Resource
    {



        /// <summary> 注：NPC 基础立场枚举，后续可扩展为阵营系统 </summary>
        public enum NpcDisposition
        {
            Enemy,      // 敌人
            Neutral,    // 中立
            Ally        // 友军
        }


        [ExportGroup("基础")]
        [Export] public string m_name = string.Empty;
        [Export] public float m_speed = 5.0f;
        [Export] public float m_rotationSpeed = 5;
        [Export] public NpcDisposition 阵营 = NpcDisposition.Enemy;

        [ExportGroup("巡逻")]
        [Export] public float m_patrolRadius = 10.0f;
        [Export] public float m_stopTime = 2.0f;

        [ExportGroup("移动和寻路")]
        [Export] public float m_targetDistance = 1.0f;



    }
}
