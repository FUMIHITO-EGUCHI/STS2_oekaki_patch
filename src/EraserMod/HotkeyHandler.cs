using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace EraserMod;

public static class HotkeyHandler
{
    private static bool _prevDecrease, _prevIncrease, _prevReset, _prevUndo;

    private static NMapDrawings _host;
    private static NMapDrawings _attachedTo;

    public static void AttachOnce(NMapDrawings host)
    {
        if (host == null) return;
        if (_attachedTo == host && GodotObject.IsInstanceValid(_attachedTo)) return;
        Bootstrap.MarkGodotReady();
        LogOverlay.EnsureCreated(host);
        Toolbar.AttachOnce(host);
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

        Toast.Show(host, $"消しゴム: {Config.EraserMultiplier:F2}x", 1.2f);
    }

    private static bool _prevToggle;
    private static bool _prevToolbarToggle;

    private static void Tick()
    {
        Toolbar.TickFrame();
        if (!Toolbar.IsMapActive())
        {
            _prevDecrease = false;
            _prevIncrease = false;
            _prevReset = false;
            _prevUndo = false;
            _prevToggle = false;
            _prevToolbarToggle = false;
            return;
        }

        bool dec = Input.IsKeyPressed(Key.Bracketleft);
        bool inc = Input.IsKeyPressed(Key.Bracketright);
        bool rst = Input.IsKeyPressed(Key.Backslash);
        bool shift = Input.IsKeyPressed(Key.Shift);
        bool ctrl = Input.IsKeyPressed(Key.Ctrl);
        bool tog = ctrl && shift && Input.IsKeyPressed(Key.L);
        bool toolbarTog = ctrl && shift && Input.IsKeyPressed(Key.E);
        bool undo = ctrl && Input.IsKeyPressed(Key.Z);

        if (tog && !_prevToggle) LogOverlay.Toggle();
        _prevToggle = tog;

        if (toolbarTog && !_prevToolbarToggle) Toolbar.Toggle();
        _prevToolbarToggle = toolbarTog;

        if (undo && !_prevUndo)
        {
            if (UndoStack.UndoLocal(_host))
            {
                NetSync.SendUndo();
                Toast.Show(_host, "Undo");
            }
        }
        _prevUndo = undo;

        if (dec && !_prevDecrease)
        {
            if (shift) Config.AdjustPencil(-Config.Step);
            else Config.AdjustEraser(-Config.Step);
            Announce(shift);
        }
        if (inc && !_prevIncrease)
        {
            if (shift) Config.AdjustPencil(+Config.Step);
            else Config.AdjustEraser(+Config.Step);
            Announce(shift);
        }
        if (rst && !_prevReset)
        {
            if (shift) Config.PencilMultiplier = 1.0f;
            else Config.EraserMultiplier = 1.0f;
            Config.Save();
            Announce(shift);
        }

        _prevDecrease = dec;
        _prevIncrease = inc;
        _prevReset = rst;
    }

    private static void Announce(bool pencil)
    {
        Toolbar.Refresh();
        var msg = pencil ? $"鉛筆: {Config.PencilMultiplier:F2}x" : $"消しゴム: {Config.EraserMultiplier:F2}x";
        Bootstrap.Log(msg);
        if (_host != null && GodotObject.IsInstanceValid(_host))
            Toast.Show(_host, msg);
    }
}
