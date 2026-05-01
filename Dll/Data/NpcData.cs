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
        [Export] public string m_name = string.Empty;
        [Export] public float m_speed  = 5.0f;
        [Export] public float m_patrolRadius = 10.0f;
        [Export] public float m_stopTime = 2.0f;
        [Export] public float m_targetDistance = 1.0f;
    }
}
