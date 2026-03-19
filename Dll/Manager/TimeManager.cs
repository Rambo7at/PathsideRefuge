namespace 维修公司.Dll
{
	/// <summary>全局时间管理器 - 游戏1天=24分钟</summary>
	public partial class TimeManager : Godot.Node
	{
		public static TimeManager Instance;

		// 核心常量：1现实秒=60游戏秒（24分钟=游戏1天）
		private const float SCALE = 86400f / 1440f;

		// 核心变量
		private float m_TotalSec = 0f; // 游戏总秒数（0-86400）
		public bool m_Paused = false;  // 是否暂停

		// 只读游戏时间
		public int Hour => (int)(m_TotalSec / 3600) % 24;
		public int Minute => (int)(m_TotalSec / 60) % 60;

		// 单例初始化
		public override void _Ready()
		{
			Instance = this;
			Pause();
			Godot.GD.Print("[TimeManager] 初始化完成");
		}

		// 时间流逝核心
		public override void _Process(double delta)
		{
			if (m_Paused) return;

			m_TotalSec += (float)delta * SCALE;
			if (m_TotalSec >= 86400f) m_TotalSec = 0f;
		}

		// 暂停/恢复
		public void Pause() => m_Paused = true;
		public void Resume() => m_Paused = false;


		// 获取时间
		public string GetTimeString() => $"{Hour:D2}:{Minute:D2}";
	}
}
