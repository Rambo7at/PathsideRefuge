using Godot;
using TerraEnvEditor.Data;

namespace TerraEnvEditor.Env;

/// <summary>注：昼夜循环（v4）——天气配置驱动：四时段（晨/日/暮/夜）插值 + Sun/Moon 双平行光 + 简化体积云</summary>
[Tool]
[GlobalClass]
public partial class SkyEditor : Node3D
{
	[Export] public DirectionalLight3D Sun;         // 第 1 个平行光 = 太阳
	[Export] public DirectionalLight3D MoonLight;   // 第 2 个平行光 = 月亮
	[Export] public WorldEnvironment WorldEnv;      // 环境（Sky = sky_simple.gdshader）

	[ExportGroup("天气配置")]
	[Export] public WeatherData Weather;         // 当前天气配置（空 = 内置默认晴天）

	[ExportGroup("雾")]
	[Export] public bool FogEnabled = false;        // 雾总开关（默认关：晴天清朗；雾天气档时勾上）

	[ExportGroup("太阳朝向")]
	[Export] public float SunYaw = 90f;            // 太阳轨道朝向（东升西落，以实测为准）

	[ExportGroup("月光")]
	[Export] public Color MoonLightColor = new(0.57f, 0.78f, 0.96f);
	[Export] public float MoonLightEnergy = 0.3f;
	[Export] public float MoonPhaseOffset = 180f;   // 月亮比太阳晚的角度（180 = 日落月出）

	[ExportGroup("云")]
	[Export] public bool CloudsEnabled = true;

	[ExportGroup("时间参数")]
	[Export] public float DayLengthMin = 24f;
	[Export] public float StartHour = 8f;
	[Export] public bool AutoRun = true;
	[Export] public float TimeScale = 1f;

	private WeatherData _weather;                // 运行时当前天气
	private float _totalSec;
	private int _lastLogHour = -1;
	private Vector2 _cumulusPos;
	private ShaderMaterial _skyMat;

	public float Hour => _totalSec / 3600f % 24f;
	public string GetTimeString() => $"{(int)Hour:D2}:{(int)(_totalSec / 60f) % 60:D2}";






	public override void _Ready()
	{
		Sun ??= GetNodeOrNull<DirectionalLight3D>("SunContainer/Sun");
		MoonLight ??= GetNodeOrNull<DirectionalLight3D>("SunContainer/Moon");
		WorldEnv ??= GetNodeOrNull<WorldEnvironment>("WorldEnvironment");

		_skyMat = WorldEnv?.Environment?.Sky?.SkyMaterial as ShaderMaterial;

		if (_skyMat != null)
		{
			_skyMat.SetShaderParameter("cumulus_visible", CloudsEnabled);
		}


		// 解析当前天气：未指定 → 内置默认晴天
		_weather = Weather ?? WeatherData.CreateDefault();

		_totalSec = StartHour * 3600f;
		GD.Print($"[DayNightCycle] 初始化完成，起始 {GetTimeString()}，天气：{_weather.WeatherName}");
	}

	public override void _Process(double delta)
	{
		// F8 按住快进（×60）
		float rate = Input.IsKeyPressed(Key.F8) ? 60f : 1f;

		if (AutoRun)
		{
			_totalSec += (float)delta * (86400f / (DayLengthMin * 60f)) * rate * TimeScale;
			if (_totalSec >= 86400f) _totalSec -= 86400f;
			if (_totalSec < 0f) _totalSec += 86400f;
		}

		float hour = Hour;
		UpdateSunMoon(hour);
		UpdateEnvironment(hour);
		UpdateClouds((float)delta * rate);

		if ((int)hour != _lastLogHour)
		{
			_lastLogHour = (int)hour;
			GD.Print($"[DayNightCycle] 时间 {GetTimeString()}");
		}
	}

	// ---------- 四时段插值（关键点：7晨 / 12日 / 17暮 / 23夜，循环） ----------

	private Color SampleColor4(Color morning, Color day, Color evening, Color night, float hour)
	{
		if (hour < 5f) return night;                                    // 0-5点：深夜
		if (hour < 6f) return night.Lerp(morning, (hour - 5f));        // 5-6点：夜→朝霞（云开始被染色）
		if (hour < 8f) return morning.Lerp(day, (hour - 6f) * 0.5f);  // 6-8点：朝霞→白天
		if (hour < 17f) return day;                                     // 8-17点：纯白天
		if (hour < 18f) return day.Lerp(evening, (hour - 17f));       // 17-18点：白天→黄昏
		if (hour < 19f) return evening.Lerp(night, (hour - 18f));      // 18-19点：黄昏→夜
		return night;                                                   // 19-24点：深夜
	}

	private float SampleFloat4(float morning, float day, float evening, float night, float hour)
	{
		if (hour < 6f) return night;
		if (hour < 7f) return Mathf.Lerp(night, morning, (hour - 6f));
		if (hour < 8f) return Mathf.Lerp(morning, day, (hour - 7f));
		if (hour < 17f) return day;
		if (hour < 18f) return Mathf.Lerp(day, evening, (hour - 17f));
		if (hour < 19f) return Mathf.Lerp(evening, night, (hour - 18f));
		return night;
	}



	/// <summary>注：外部设定当前时间（秒，0~86400），由主项目的 GameTime 每帧注入</summary>
	public void SetTime(float totalSec)
	{
		_totalSec = Mathf.Wrap(totalSec, 0f, 86400f);
	}

	private float GetSunAngle() => Hour * 15f;

	private float GetDayFactor()
	{
		float day = Mathf.Clamp(-Mathf.Cos(Mathf.DegToRad(GetSunAngle())), 0f, 1f);
		if (_weather.AlwaysDark) day *= 0.35f;   // 暴雨/雷暴：全天昏暗
		return day;
	}

	private void UpdateSunMoon(float hour)
	{
		// 太阳光颜色（天气配置驱动，四时段插值）
		Color sunColor = SampleColor4(
			_weather.SunColorMorning, _weather.SunColorDay,
			_weather.SunColorEvening, _weather.SunColorNight, hour);

		float day = GetDayFactor();

		if (Sun != null)
		{
			Sun.RotationDegrees = new Vector3(GetSunAngle(), SunYaw, 0f);

			if (day > 0.02f)
			{
				Sun.LightColor = sunColor;
				Sun.LightEnergy = day * _weather.LightIntensityDay;
				Sun.ShadowEnabled = day > 0.08f;
			}
			else
			{
				Sun.LightEnergy = 0f;
				Sun.ShadowEnabled = false;
			}

			// 云 tint 跟随太阳光颜色
			_skyMat?.SetShaderParameter("sun_light_color", sunColor);
			// 太阳圆盘颜色跟随配置
			_skyMat?.SetShaderParameter("sun_disk_color", sunColor);
		}

		if (MoonLight != null)
		{
			float mx = (GetSunAngle() + MoonPhaseOffset) % 360f;
			MoonLight.RotationDegrees = new Vector3(mx, SunYaw, 0f);

			float mday = Mathf.Clamp(-Mathf.Cos(Mathf.DegToRad(mx)), 0f, 1f);
			if (mday > 0.05f)
			{
				MoonLight.LightColor = MoonLightColor;
				MoonLight.LightEnergy = MoonLightEnergy * mday * _weather.LightIntensityNight;
				MoonLight.ShadowEnabled = true;
			}
			else
			{
				MoonLight.LightEnergy = 0f;
				MoonLight.ShadowEnabled = false;
			}
		}
	}

	private void UpdateEnvironment(float hour)
	{
		if (WorldEnv?.Environment is not Environment env) return;

		float day = GetDayFactor();

		// 环境光（day/night 两色插值）
		env.AmbientLightColor = _weather.AmbColorDay.Lerp(_weather.AmbColorNight, 1f - day);
		env.AmbientLightEnergy = _weather.AmbientVol;

		// 雾：颜色 + 密度（四时段插值），由 FogEnabled 总开关控制
		env.FogEnabled = FogEnabled;
		env.FogLightColor = SampleColor4(
			_weather.FogColorMorning, _weather.FogColorDay,
			_weather.FogColorEvening, _weather.FogColorNight, hour);
		env.FogDensity = SampleFloat4(
			_weather.FogAmountMorning, _weather.FogAmountDay,
			_weather.FogAmountEvening, _weather.FogAmountNight, hour) * 0.0001f;
		env.FogSkyAffect = 0;
	}

	private void UpdateClouds(float delta)
	{
		if (_skyMat == null) return;

		// 云参数全部来自天气配置
		_skyMat.SetShaderParameter("cumulus_coverage", _weather.CloudCoverage);
		_skyMat.SetShaderParameter("cumulus_thickness", _weather.CloudThickness);
		_skyMat.SetShaderParameter("cumulus_intensity", _weather.CloudIntensity);
		_skyMat.SetShaderParameter("cumulus_absorption", _weather.CloudAbsorption);

		// 风：WindMin ~ WindMax 之间缓慢波动，方向来自配置
		float t = Time.GetTicksMsec() * 0.001f;
		float wind = Mathf.Lerp(_weather.WindMin, _weather.WindMax, 0.5f + 0.5f * Mathf.Sin(t * 0.15f));
		_cumulusPos += _weather.CloudWindDirection * (wind * 0.01f) * delta;
		_skyMat.SetShaderParameter("cumulus_position", _cumulusPos);
	}
}
