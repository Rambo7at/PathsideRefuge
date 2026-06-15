

using Godot;
using Godot.Collections;
using 维修公司.Dll.data;
using 途畔归所.Dll.Base;
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

        [ExportGroup("基础属性")]
        
        [Export] public string m_ID { get; set; }
        [Export] public string m_name { get; set; } = string.Empty;
        [Export] public string m_description { get; set; } = string.Empty;
        [Export] public Texture2D m_icon { get; set; }
        [Export] public VegetationType m_type { get; set; }

        [ExportGroup("掉落")]
        [Export] public Array<DropBase> m_dropTable { get; set; }


        [ExportGroup("生存属性")]
        [Export] public float m_health = 100f;

        [ExportGroup("自定义数据")]
        [Export] public Variant m_data { get; set; }


        public VegetationData DeepCopy() => this.DuplicateDeep() as VegetationData;
    }
}
