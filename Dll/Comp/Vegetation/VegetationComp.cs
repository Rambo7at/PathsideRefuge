


using Godot;
using Godot.Collections;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Comp
{
    public partial class VegetationComp : EntityBase
    {
        [Export] public VegetationData m_VegetationData { get; set; }
        public float m_Health { get => m_VegetationData.m_Health; set => m_VegetationData.m_Health = value; }



        public virtual void Die(Node node)
        {
            if (m_Health <= 0) CatUtils.StopAndExit(node);
        }




    }
}
