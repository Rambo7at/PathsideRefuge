using Godot;


namespace TerraEnvEditor.Data;
/// <summary>注：单个杂物配置资源——可在编辑器创建 .tres，mesh、材质权重过滤、数量、海拔/坡度/分形多层过滤</summary>
[GlobalClass]
[Tool]
public partial class ClutterData : Resource
{
    [Export] public string Name { get; set; } = "";
    [Export] public bool Enabled { get; set; } = true;
    [Export] public Mesh Mesh { get; set; }
    [Export] public int Amount { get; set; } = 50;
    [Export] public float ScaleMin { get; set; } = 1f;
    [Export] public float ScaleMax { get; set; } = 1f;
    [Export] public float MaxTilt { get; set; } = 45f;
    [Export] public float MinAltitude { get; set; } = -1000f;
    [Export] public float MaxAltitude { get; set; } = 1000f;
    [Export] public bool TerrainTilt { get; set; } = true;
    [Export] public float FractalScale { get; set; } = 0f;
    [Export] public float FractalThresholdMin { get; set; } = 0.5f;
    [Export] public float FractalThresholdMax { get; set; } = 1f;
    [Export] public float GrassWeightMin { get; set; } = -1f;
    [Export] public float GrassWeightMax { get; set; } = -1f;
}
