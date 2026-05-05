

using Godot;
using static PlacedData;

namespace 途畔归所.Dll.Data
{
    [GlobalClass]
    public partial class VegetationData : Resource
    {
        public enum VegetationType
        {
            树木,
            石头
        }

        [Export] public string m_ID { get; set; }
        [Export] public string m_Name { get; set; } = string.Empty;
        [Export] public VegetationType m_Type { get; set; }

        [Export] public float m_Health = 100f;

    }
}
