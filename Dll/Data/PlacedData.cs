using Godot;
using System;
using System.Runtime.CompilerServices;
using static 维修公司.Dll.data.ItemData;

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







}
