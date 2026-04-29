using Godot;

namespace 途畔归所.Dll.Manager
{
	/// <summary>全局时间管理器 - 游戏1天=24分钟</summary>
	public partial class TimeManager : Node
	{
        public static TimeManager Instance { get;  set; }
        // 时间倍率
        private const float RATE = 86400f / 1440f;

        // 游戏总秒数（0-86400）
        private float m_TotalSec = 0f;
        // 是否暂停
        private bool m_Pause = false;  

		// 游戏时间
		public int Hour => (int)(m_TotalSec / 3600) % 24;
		public int Minute => (int)(m_TotalSec / 60) % 60;

		public override void _Ready()
		{
            Instance = this;
            Pause();
			GD.Print("[TimeManager] 初始化完成");
		}

		// 时间流逝核心
		public override void _Process(double delta)
		{
			if (m_Pause) return;

			m_TotalSec += (float)delta * RATE;
			if (m_TotalSec >= 86400f) m_TotalSec = 0f;
		}

		// 暂停/恢复
		public void Pause() => m_Pause = true;
		public void Resume() => m_Pause = false;


		// 获取时间
		public string GetTimeString() => $"{Hour:D2}:{Minute:D2}";
	}
}
