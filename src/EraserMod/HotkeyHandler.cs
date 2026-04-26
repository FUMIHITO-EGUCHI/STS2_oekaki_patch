using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace EraserMod;

public static class HotkeyHandler
{
    private static bool _prevDecrease, _prevIncrease, _prevReset;

    private static NMapDrawings _host;
    private static NMapDrawings _attachedTo;

    public static void AttachOnce(NMapDrawings host)
    {
        if (host == null) return;
        if (_attachedTo == host && GodotObject.IsInstanceValid(_attachedTo)) return;
        Bootstrap.MarkGodotReady();
        LogOverlay.EnsureCreated(host);
        Attach(host);
        _attachedTo = host;
    }

    public static void Attach(NMapDrawings host)
    {
        var tree = host.GetTree();
        if (tree == null) return;

        _host = host;
        Action handler = Tick;
        tree.ProcessFrame += handler;
        host.TreeExiting += () =>
        {
            try { tree.ProcessFrame -= handler; } catch { }
            if (_host == host) _host = null;
        };

        // Show current setting briefly when the map opens.
        Toast.Show(host, $"消しゴム: {Config.WidthMultiplier:F2}x", 1.2f);
    }

    private static bool _prevToggle;

    private static void Tick()
    {
        bool dec = Input.IsKeyPressed(Key.Bracketleft);
        bool inc = Input.IsKeyPressed(Key.Bracketright);
        bool rst = Input.IsKeyPressed(Key.Backslash);
        bool tog = Input.IsKeyPressed(Key.F8);

        if (tog && !_prevToggle) LogOverlay.Toggle();
        _prevToggle = tog;

        if (dec && !_prevDecrease)
        {
            Config.Adjust(-Config.Step);
            Announce();
        }
        if (inc && !_prevIncrease)
        {
            Config.Adjust(+Config.Step);
            Announce();
        }
        if (rst && !_prevReset)
        {
            Config.WidthMultiplier = 1.0f;
            Config.Save();
            Announce();
        }

        _prevDecrease = dec;
        _prevIncrease = inc;
        _prevReset = rst;
    }

    private static void Announce()
    {
        var msg = $"消しゴム: {Config.WidthMultiplier:F2}x";
        Bootstrap.Log(msg);
        if (_host != null && GodotObject.IsInstanceValid(_host))
            Toast.Show(_host, msg);
    }
}
