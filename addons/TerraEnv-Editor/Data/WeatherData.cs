using Godot;

namespace TerraEnvEditor.Data;

/// <summary>注：天气配置（仿英灵神殿 Valheim weather）——一个天气 = 一组时段参数，可存为 .tres 资源复用、多天气切换</summary>
[GlobalClass]
[Tool]
public partial class WeatherData : Resource
{
    // ===== 基础 =====
    [Export] public string WeatherName = "Clear";
    [Export(PropertyHint.Enum, "None,HeathClear,Misty,Rain,ThunderStorm,Snow")]
    public string Particles = "HeathClear";      // 粒子类型（未来接雨/雪系统）
    [Export] public bool IsDefault = false;       // 是否默认天气
    [Export] public bool IsWet = false;           // 是否淋湿（玩法用）
    [Export] public bool IsColdAtNight = false;   // 夜晚寒冷（玩法用）
    [Export] public bool AlwaysDark = false;      // 总是昏暗（暴雨/雷暴，已生效）

    // ===== 风 =====
    [ExportGroup("风")]
    [Export(PropertyHint.Range, "0,2,0.01")] public float WindMin = 0.1f;
    [Export(PropertyHint.Range, "0,3,0.01")] public float WindMax = 0.6f;

    // ===== 云 =====
    [ExportGroup("云")]
    [Export(PropertyHint.Range, "0,3,0.01")] public float RainCloudAlpha = 0f;    // 雨云不透明度（0=晴）
    [Export(PropertyHint.Range, "0,1,0.01")] public float CloudCoverage = 0.55f;  // 积云覆盖（shader cumulus_coverage）
    [Export] public Vector2 CloudWindDirection = new(0.25f, 0.15f);               // 云移动方向
    [Export(PropertyHint.Range, "0,0.2,0.001")] public float CloudThickness = 0.0243f;  // 云层厚度（立体感）
    [Export(PropertyHint.Range, "0,8,0.1")] public float CloudIntensity = 1.0f;         // 云亮度
    [Export(PropertyHint.Range, "0,8,0.1")] public float CloudAbsorption = 2.0f;        // 云不透明度

    // ===== 环境光 =====
    [ExportGroup("环境光")]
    [Export(PropertyHint.Range, "0,2,0.01")] public float AmbientVol = 0.25f;     // 环境光强度
    [Export] public Color AmbColorDay = new(0.50f, 0.53f, 0.52f);
    [Export] public Color AmbColorNight = new(0.25f, 0.25f, 0.40f);

    // ===== 太阳 =====
    [ExportGroup("太阳")]
    [Export] public Color SunColorMorning = new(1.0f, 0.70f, 0.50f);
    [Export] public Color SunColorDay = new(0.98f, 0.86f, 0.70f);
    [Export] public Color SunColorEvening = new(0.90f, 0.50f, 0.40f);
    [Export] public Color SunColorNight = new(0.35f, 0.30f, 0.45f);
    [Export(PropertyHint.Range, "0,90,1")] public float SunAngle = 45f;           // 太阳正午最大仰角（预留）

    // ===== 雾 =====
    [ExportGroup("雾")]
    [Export] public Color FogColorMorning = new(0.38f, 0.42f, 0.44f);
    [Export] public Color FogColorDay = new(0.46f, 0.52f, 0.55f);
    [Export] public Color FogColorEvening = new(0.40f, 0.35f, 0.45f);
    [Export] public Color FogColorNight = new(0.20f, 0.20f, 0.28f);
    // 雾中太阳颜色（英灵神殿字段；Godot 原生雾暂无对应，预留）
    [Export] public Color FogColorSunMorning = new(0.78f, 0.58f, 0.42f);
    [Export] public Color FogColorSunDay = new(0.88f, 0.78f, 0.62f);
    [Export] public Color FogColorSunEvening = new(0.70f, 0.45f, 0.35f);
    [Export] public Color FogColorSunNight = new(0.25f, 0.25f, 0.35f);
    [Export(PropertyHint.Range, "0,100,0.5")] public float FogAmountMorning = 30f;
    [Export(PropertyHint.Range, "0,100,0.5")] public float FogAmountDay = 20f;
    [Export(PropertyHint.Range, "0,100,0.5")] public float FogAmountEvening = 40f;
    [Export(PropertyHint.Range, "0,100,0.5")] public float FogAmountNight = 60f;

    // ===== 光照强度 =====
    [ExportGroup("光照强度")]
    [Export(PropertyHint.Range, "0,3,0.01")] public float LightIntensityDay = 1.35f;
    [Export(PropertyHint.Range, "0,3,0.01")] public float LightIntensityNight = 0.6f;

    /// <summary>注：内置默认晴天（参考英灵神殿 wilderness1 数值）</summary>
    public static WeatherData CreateDefault()
    {
        return new WeatherData
        {
            WeatherName = "Clear",
            Particles = "HeathClear",
            IsDefault = true,
            WindMin = 0.1f,
            WindMax = 0.6f,
            RainCloudAlpha = 0f,
            CloudCoverage = 0.55f,
            CloudWindDirection = new Vector2(0.25f, 0.15f),
            CloudThickness = 0.0243f,
            CloudIntensity = 1.0f,
            CloudAbsorption = 2.0f,
            AmbientVol = 0.25f,
            AmbColorDay = new Color(0.50f, 0.53f, 0.52f),
            AmbColorNight = new Color(0.25f, 0.25f, 0.40f),
            SunColorMorning = new Color(1.0f, 0.70f, 0.50f),
            SunColorDay = new Color(0.98f, 0.86f, 0.70f),
            SunColorEvening = new Color(0.90f, 0.50f, 0.40f),
            SunColorNight = new Color(0.35f, 0.30f, 0.45f),
            SunAngle = 45f,
            FogColorMorning = new Color(0.38f, 0.42f, 0.44f),
            FogColorDay = new Color(0.46f, 0.52f, 0.55f),
            FogColorEvening = new Color(0.40f, 0.35f, 0.45f),
            FogColorNight = new Color(0.20f, 0.20f, 0.28f),
            FogAmountMorning = 30f,
            FogAmountDay = 20f,
            FogAmountEvening = 40f,
            FogAmountNight = 60f,
            LightIntensityDay = 1.35f,
            LightIntensityNight = 0.6f,
        };
    }
}
