using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 途畔归所.Dll.Base
{
    public partial class EntityBase : StaticBody3D
    {

        [Export] public string m_EntityGUID { get; set; }

        public Dictionary<string, Variant> CustomData = [];


        public override void _Ready()
        { 
        
        }


        public override void _Process(double delta)
        {
        }


        public void InitEntityBase()
        {
            if (!string.IsNullOrEmpty(m_EntityGUID)) return;

            m_EntityGUID = Guid.NewGuid().ToString();
        }

    }
}
