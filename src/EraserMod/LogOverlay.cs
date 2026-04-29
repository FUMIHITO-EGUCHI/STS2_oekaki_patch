using System.Collections.Generic;
using Godot;

namespace EraserMod;

public static class LogOverlay
{
    private static RichTextLabel _label;
    private static CanvasLayer _layer;
    private static readonly List<string> _lines = new();
    private const int MaxLines = 25;
    private static bool _visible;

    public static void EnsureCreated(Node anyHost)
    {
        if (_label != null && GodotObject.IsInstanceValid(_label)) return;
        var tree = anyHost.GetTree();
        if (tree?.Root == null) return;

        _layer = new CanvasLayer { Name = "EraserModLogLayer", Layer = 127 };
        tree.Root.AddChild(_layer);

        _label = new RichTextLabel
        {
            Name = "EraserModLog",
            BbcodeEnabled = false,
            FitContent = true,
            ScrollFollowing = true,
            Visible = _visible,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _label.AnchorLeft = 0f;
        _label.AnchorRight = 0f;
        _label.AnchorTop = 1f;
        _label.AnchorBottom = 1f;
        _label.OffsetLeft = 12f;
        _label.OffsetRight = 720f;
        _label.OffsetTop = -360f;
        _label.OffsetBottom = -12f;

        _label.AddThemeColorOverride("default_color", new Color(0.85f, 1f, 0.85f, 0.95f));
        _label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f));
        _label.AddThemeConstantOverride("outline_size", 4);
        _label.AddThemeFontSizeOverride("normal_font_size", 14);

        _layer.AddChild(_label);
        Refresh();
    }

    public static void Append(string line)
    {
        lock (_lines)
        {
            _lines.Add(line);
            while (_lines.Count > MaxLines) _lines.RemoveAt(0);
        }
        Refresh();
    }

    public static void Toggle()
    {
        _visible = !_visible;
        if (_label != null && GodotObject.IsInstanceValid(_label)) _label.Visible = _visible;
    }

    private static void Refresh()
    {
        if (_label == null || !GodotObject.IsInstanceValid(_label)) return;
        string joined;
        lock (_lines) joined = string.Join("\n", _lines);
        _label.Text = joined;
    }
}
