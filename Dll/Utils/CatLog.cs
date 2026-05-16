using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 途畔归所.Dll.Utils
{
    public static class CatLog
    {
        public static bool onInfo = false;
        public static bool onOk = false;
        public static bool onWarn = false;
        public static bool onErr = false;
        public static bool onDebug = false;
        public static bool onNet = true;
        public static void Info(string msg) { if (onInfo) GD.Print(msg); }
        public static void Ok(string msg) { if (onOk) GD.PrintRich($"[color=green]{msg}[/color]"); }
        public static void Warn(string msg) { if (onWarn) GD.PrintRich($"[color=yellow]{msg}[/color]"); }
        public static void Err(string msg) { if (onErr) GD.PrintErr(msg); }

        public static void Net(string msg) { if (onNet) GD.PrintRich($"[color=#9932CC]{msg}[/color]"); }
        public static void Debug(string msg) { if (onDebug) GD.PrintRich($"[color=#778899]{msg}[/color]"); }
    }
}
