using System;
using Godot;

namespace EraserMod;

public static class Toast
{
    private static Label _label;
    private static ulong _hideAtMsec;
    private static CanvasLayer _layer;

    public static void Show(Node host, string text, float seconds = 1.5f)
    {
        try
        {
            EnsureLabel(host);
            if (_label == null) return;

            _label.Text = text;
            _label.Visible = true;
            _hideAtMsec = Time.GetTicksMsec() + (ulong)(seconds * 1000f);
        }
        catch (Exception e)
        {
            Bootstrap.Log("Toast.Show error: " + e.Message);
        }
    }

    private static void EnsureLabel(Node host)
    {
        if (_label != null && GodotObject.IsInstanceValid(_label)) return;

        var tree = host.GetTree();
        if (tree?.Root == null) return;

        // CanvasLayer with high layer index so we render above the map UI.
        _layer = new CanvasLayer
        {
            Layer = 128,
            Name = "EraserModToastLayer"
        };
        tree.Root.AddChild(_layer);

        var label = new Label
        {
            Name = "EraserModToast",
            Text = "",
            Visible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f));
        label.AddThemeConstantOverride("outline_size", 8);
        label.AddThemeFontSizeOverride("font_size", 32);

        // Anchor: top-center of viewport.
        label.AnchorLeft = 0.5f;
        label.AnchorRight = 0.5f;
        label.AnchorTop = 0f;
        label.AnchorBottom = 0f;
        label.OffsetLeft = -200f;
        label.OffsetRight = 200f;
        label.OffsetTop = 40f;
        label.OffsetBottom = 100f;

        _layer.AddChild(label);
        _label = label;

        // Drive auto-hide from the scene tree's process_frame.
        tree.ProcessFrame += OnProcessFrame;
    }

    private static void OnProcessFrame()
    {
        if (_label == null || !GodotObject.IsInstanceValid(_label)) return;
        if (_label.Visible && Time.GetTicksMsec() >= _hideAtMsec)
        {
            _label.Visible = false;
        }
    }
}
