using Godot;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.System;

/// <summary>注：游戏时间系统，管理全局游戏时间推进与昼夜状态；时间数值由 WorldData.TimeOfDay 持久化，本类只负责运行时</summary>
public class GameTime
{
    /// <summary>注：游戏运行状态</summary>
    public enum E_GameState
    {
        Menu = 0,      // 主菜单/角色创建，时间暂停
        Playing = 1,   // 玩家进入世界开始行动，时间推进
    }

    /// <summary>注：时间倍率，1真实秒 = 60游戏秒（1天=24分钟）</summary>
    private const float TIME_RATE = 86400f / 1440f;

    /// <summary>注：新游戏起始时刻（08:00 对应秒数）</summary>
    public const float START_HOUR_SEC = 8f * 3600f;

    /// <summary>注：游戏总秒数（0~86400）</summary>
    private float _totalSec = 0f;

    /// <summary>注：上次日志记录的游戏小时，用于小时变化时打印一次</summary>
    private int _lastLogHour = -1;

    /// <summary>注：当前游戏状态（仅 Playing 时时间推进）</summary>
    public E_GameState GameState { get; private set; } = E_GameState.Menu;

    /// <summary>注：当前游戏小时（0-23）</summary>
    public int Hour => (int)(_totalSec / 3600) % 24;

    /// <summary>注：当前游戏分钟（0-59）</summary>
    public int Minute => (int)(_totalSec / 60) % 60;

    /// <summary>注：单例实例</summary>
    private static GameTime _instance;
    /// <summary>注：单例访问入口，首次访问时自动创建</summary>
    public static GameTime Instance => _instance ??= new GameTime();

    /// <summary>注：时间推进，仅 Playing 状态生效；由 GameCore 每帧调用</summary>
    public void UpdateTime(float delta)
    {
        if (GameState != E_GameState.Playing) return;

        _totalSec += delta * TIME_RATE;
        if (_totalSec >= 86400f) _totalSec -= 86400f;

        // 游戏小时变化时打印一次（1游戏小时 = 1真实分钟，便于快速测试）
        if (Hour != _lastLogHour)
        {
            _lastLogHour = Hour;
            CatLog.Debug($"[Time] 游戏时间：{GetTimeString()}");
        }
    }

    /// <summary>注：设置当前时间（秒数，0~86400），进入世界时从 WorldData 存档注入</summary>
    public void SetTime(float totalSec)
    {
        _totalSec = Mathf.Wrap(totalSec, 0f, 86400f);
        _lastLogHour = -1;
    }

    /// <summary>注：获取当前时间（秒数，0~86400），保存时写回 WorldData 存档</summary>
    public float GetTime() => _totalSec;

    /// <summary>注：游戏开始，进入 Playing 状态（幂等，重复进入不重置时间；时间数值由 WorldManager 从存档提前注入）</summary>
    public void StartGame()
    {
        if (GameState == E_GameState.Playing) return;
        GameState = E_GameState.Playing;
        CatLog.Ok($"[Time] 游戏开始，当前时间：{GetTimeString()}");
    }

    /// <summary>注：返回主菜单，时间暂停</summary>
    public void ReturnToMenu()
    {
        GameState = E_GameState.Menu;
        CatLog.Ok("[Time] 返回主菜单，时间暂停");
    }

    /// <summary>注：获取当前时间字符串（HH:mm）</summary>
    public string GetTimeString() => $"{Hour:D2}:{Minute:D2}";
}