using Godot;
using 途畔归所.Dll.Data;

[GlobalClass]
public partial class PlacedData : Resource
{

    public enum PlacedType
    {
        建筑,
        容器,
        工作台
    }

    [Export] public string m_ID { get; set; }

    [Export] public string m_Name { get; set; } = string.Empty;

    [Export] public string m_Description { get; set; }
    [Export] public Texture2D m_Icon { get; set; }
    [Export] public PlacedType m_Type { get; set; }

    [Export] public Variant m_data { get; set; }


    public PlacedData DeepCopy() => this.DuplicateDeep() as PlacedData;


}
