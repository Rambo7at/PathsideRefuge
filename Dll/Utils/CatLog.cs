using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Core;

namespace 途畔归所.Dll.Utils;

public static class CatLog
{
    public static bool onInfo = true;
    public static bool onOk = true;
    public static bool onWarn = true;
    public static bool onErr = true;
    public static bool onNet = true;
    public static bool onDebug = true;

    public static void Info(string msg)
    {
        if (!onInfo) return;
        string peerid = string.Empty;
        if (NetCore.Instance != null && NetCore.Instance.IsMultiplayer)
        {
            peerid = $"[{NetCore.Instance.LocalPeerID}]";
        }
        GD.Print($"{peerid}{msg}");
    }

    public static void Ok(string msg)
    {
        if (!onOk) return;
        string peerid = string.Empty;
        if (NetCore.Instance != null && NetCore.Instance.IsMultiplayer)
        {
            peerid = $"[{NetCore.Instance.LocalPeerID}]";
        }
        GD.PrintRich($"[color=green]{peerid}{msg}[/color]");
    }

    public static void Warn(string msg)
    {
        if (!onWarn) return;
        GD.PrintRich($"[color=yellow]{msg}[/color]");
    }

    public static void Err(string msg)
    {
        if (!onErr) return;
        string peerid = string.Empty;
        if (NetCore.Instance != null && NetCore.Instance.IsMultiplayer)
        {
            peerid = $"[{NetCore.Instance.LocalPeerID}]";
        }
        GD.PrintErr($"{peerid}{msg}");
    }

    public static void Net(string msg)
    {
        if (!onNet) return;
        GD.PrintRich($"[color=#9932CC][{NetCore.Instance.LocalPeerID}]{msg}[/color]");
    }

    public static void Debug(string msg)
    {
        if (!onDebug) return;
        GD.PrintRich($"[color=#778899]{msg}[/color]");
    }
}
