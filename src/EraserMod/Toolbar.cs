using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace EraserMod;

public static class Toolbar
{
    private const float DefaultMargin = 28f;
    private static readonly Vector2 DefaultPanelSize = new(280f, 188f);
    private static CanvasLayer _layer;
    private static PanelContainer _panel;
    private static HSlider _eraserSlider;
    private static HSlider _pencilSlider;
    private static Label _eraserLabel;
    private static Label _pencilLabel;
    private static CursorPreview _preview;
    private static NMapDrawings _host;
    private static readonly List<(Button Button, string Color)> ColorButtons = new();
    private static bool _dragging;
    private static Vector2 _dragOffset;

    public static void AttachOnce(NMapDrawings host)
    {
        if (host == null) return;
        _host = host;
        if (_panel != null && GodotObject.IsInstanceValid(_panel))
        {
            EnsurePreview(host);
            Refresh();
            return;
        }

        var tree = host.GetTree();
        if (tree?.Root == null) return;

        _layer = new CanvasLayer { Name = "EraserModToolbarLayer", Layer = 1000 };
        tree.Root.AddChild(_layer);
        Bootstrap.Log("toolbar layer attached");

        _panel = new PanelContainer
        {
            Name = "EraserModToolbar",
            Visible = IsMapActive() && Config.ToolbarVisible,
            MouseFilter = Control.MouseFilterEnum.Stop,
            CustomMinimumSize = DefaultPanelSize
        };
        SetDefaultPosition();
        _panel.AddThemeStyleboxOverride("panel", PanelStyle());
        _panel.GuiInput += OnPanelInput;
        _layer.AddChild(_panel);

        var root = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(250f, 160f)
        };
        root.AddThemeConstantOverride("separation", 7);
        _panel.AddChild(root);

        _eraserLabel = new Label();
        StyleLabel(_eraserLabel);
        root.AddChild(_eraserLabel);
        _eraserSlider = CreateSlider(Config.EraserMultiplier, v =>
        {
            Config.EraserMultiplier = Config.ClampStep((float)v);
            Config.Save();
            Refresh();
        });
        root.AddChild(_eraserSlider);

        _pencilLabel = new Label();
        StyleLabel(_pencilLabel);
        root.AddChild(_pencilLabel);
        _pencilSlider = CreateSlider(Config.PencilMultiplier, v =>
        {
            Config.PencilMultiplier = Config.ClampStep((float)v);
            Config.Save();
            Refresh();
        });
        root.AddChild(_pencilSlider);

        var colors = new HBoxContainer();
        colors.AddThemeConstantOverride("separation", 5);
        root.AddChild(colors);
        AddDefaultColorButton(colors);
        AddColorButton(colors, "#FFFFFF");
        AddColorButton(colors, "#111111");
        AddColorButton(colors, "#E4423E");
        AddColorButton(colors, "#E58B2A");
        AddColorButton(colors, "#E3C84D");
        AddColorButton(colors, "#5AA45C");
        AddColorButton(colors, "#4F83BD");
        AddColorButton(colors, "#8753A4");

        var undo = new Button { Text = "Undo", CustomMinimumSize = new Vector2(96f, 28f) };
        undo.AddThemeStyleboxOverride("normal", ButtonStyle(new Color(0.12f, 0.18f, 0.2f, 0.92f)));
        undo.AddThemeStyleboxOverride("hover", ButtonStyle(new Color(0.20f, 0.29f, 0.31f, 0.96f)));
        undo.Pressed += () => UndoStack.UndoLocal(_host);
        root.AddChild(undo);

        EnsurePreview(host);

        host.TreeExiting += Cleanup;
        Refresh();
    }

    public static void Toggle()
    {
        Config.ToolbarVisible = !Config.ToolbarVisible;
        Config.Save();
        ApplyVisibility();
    }

    public static void Refresh()
    {
        if (_eraserLabel != null && GodotObject.IsInstanceValid(_eraserLabel))
            _eraserLabel.Text = $"Eraser: x{Config.EraserMultiplier:F2}";
        if (_pencilLabel != null && GodotObject.IsInstanceValid(_pencilLabel))
            _pencilLabel.Text = $"Pencil: x{Config.PencilMultiplier:F2}";
        if (_eraserSlider != null && GodotObject.IsInstanceValid(_eraserSlider))
            _eraserSlider.Value = Config.EraserMultiplier;
        if (_pencilSlider != null && GodotObject.IsInstanceValid(_pencilSlider))
            _pencilSlider.Value = Config.PencilMultiplier;
        foreach (var (button, color) in ColorButtons)
        {
            if (button == null || !GodotObject.IsInstanceValid(button)) continue;
            var selected = Config.NormalizeHex(Config.PencilColorHex) == Config.NormalizeHex(color);
            if (color == null) selected = Config.NormalizeHex(Config.PencilColorHex) == null;
            ApplySwatchStyle(button, color, selected);
        }
        ApplyVisibility();
    }

    public static void TickPreview()
    {
        if (_host == null || !GodotObject.IsInstanceValid(_host)) return;
        EnsurePreview(_host);
        if (_preview != null && GodotObject.IsInstanceValid(_preview))
            _preview.UpdateFrame();
    }

    public static void TickFrame()
    {
        TickPreview();
        ApplyVisibility();
    }

    public static bool IsMapActive()
    {
        try
        {
            if (_host == null || !GodotObject.IsInstanceValid(_host) || !_host.IsInsideTree())
                return false;

            var map = NMapScreen.Instance;
            return map != null && GodotObject.IsInstanceValid(map) && map.IsOpen;
        }
        catch
        {
            return false;
        }
    }

    private static void ApplyVisibility()
    {
        var visible = IsMapActive() && Config.ToolbarVisible;
        if (_panel != null && GodotObject.IsInstanceValid(_panel)) _panel.Visible = visible;
    }

    private static void EnsurePreview(NMapDrawings host)
    {
        if (_layer == null || !GodotObject.IsInstanceValid(_layer)) return;
        if (_preview != null && GodotObject.IsInstanceValid(_preview))
        {
            _preview.UpdateHost(host);
            return;
        }

        _preview = new CursorPreview();
        _preview.Attach(host);
        _layer.AddChild(_preview);
        _layer.MoveChild(_preview, _layer.GetChildCount() - 1);
    }

    private static void SetDefaultPosition()
    {
        if (_panel == null) return;
        _panel.AnchorLeft = 1f;
        _panel.AnchorRight = 1f;
        _panel.AnchorTop = 1f;
        _panel.AnchorBottom = 1f;
        _panel.OffsetLeft = -(DefaultPanelSize.X + DefaultMargin);
        _panel.OffsetRight = -DefaultMargin;
        _panel.OffsetTop = -(DefaultPanelSize.Y + DefaultMargin);
        _panel.OffsetBottom = -DefaultMargin;
    }

    private static HSlider CreateSlider(float value, Action<double> onChanged)
    {
        var slider = new HSlider
        {
            MinValue = Config.Min,
            MaxValue = Config.Max,
            Step = Config.Step,
            Value = value,
            CustomMinimumSize = new Vector2(230f, 20f)
        };
        slider.ValueChanged += value => onChanged(value);
        return slider;
    }

    private static void AddDefaultColorButton(HBoxContainer parent)
    {
        var button = new Button
        {
            Text = "D",
            TooltipText = "Default color",
            CustomMinimumSize = new Vector2(24f, 24f)
        };
        button.AddThemeFontSizeOverride("font_size", 13);
        button.AddThemeColorOverride("font_color", new Color(0.92f, 0.95f, 0.90f, 1f));
        button.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.03f, 0.03f, 1f));
        button.AddThemeConstantOverride("outline_size", 3);
        ApplySwatchStyle(button, null, Config.PencilColorHex == null);
        button.Pressed += () =>
        {
            Config.PencilColorHex = null;
            Config.Save();
            Refresh();
        };
        ColorButtons.Add((button, null));
        parent.AddChild(button);
    }

    private static void AddColorButton(HBoxContainer parent, string color)
    {
        var button = new Button
        {
            Text = "",
            TooltipText = color,
            CustomMinimumSize = new Vector2(24f, 24f)
        };
        ApplySwatchStyle(button, color, Config.NormalizeHex(Config.PencilColorHex) == Config.NormalizeHex(color));
        button.Pressed += () =>
        {
            Config.PencilColorHex = Config.NormalizeHex(color);
            Config.Save();
            Refresh();
        };
        ColorButtons.Add((button, color));
        parent.AddChild(button);
    }

    private static void StyleLabel(Label label)
    {
        label.AddThemeColorOverride("font_color", new Color(0.88f, 0.94f, 0.90f, 1f));
        label.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.03f, 0.03f, 1f));
        label.AddThemeConstantOverride("outline_size", 4);
        label.AddThemeFontSizeOverride("font_size", 15);
    }

    private static StyleBoxFlat PanelStyle()
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.12f, 0.13f, 0.78f),
            BorderColor = new Color(0.45f, 0.61f, 0.64f, 0.65f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 12f,
            ContentMarginRight = 12f,
            ContentMarginTop = 10f,
            ContentMarginBottom = 10f
        };
        style.SetBorderWidthAll(2);
        return style;
    }

    private static StyleBoxFlat ButtonStyle(Color bg)
    {
        var style = new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = new Color(0.53f, 0.66f, 0.68f, 0.65f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        style.SetBorderWidthAll(1);
        return style;
    }

    private static StyleBoxFlat SwatchStyle(string hex, bool selected)
    {
        var bg = new Color(0.42f, 0.46f, 0.43f, 1f);
        if (ColorUtil.TryParseHex(hex, out var parsed)) bg = parsed;
        var style = new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = selected ? new Color(0.95f, 0.86f, 0.42f, 1f) : new Color(0.03f, 0.04f, 0.04f, 0.95f),
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3
        };
        style.SetBorderWidthAll(selected ? 3 : 1);
        return style;
    }

    private static void ApplySwatchStyle(Button button, string color, bool selected)
    {
        var style = SwatchStyle(color, selected);
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style);
        button.AddThemeStyleboxOverride("pressed", style);
        button.AddThemeStyleboxOverride("focus", style);
    }

    private static void Cleanup()
    {
        try
        {
            if (_layer != null && GodotObject.IsInstanceValid(_layer))
                _layer.QueueFree();
        }
        catch (Exception e)
        {
            Bootstrap.Log("toolbar cleanup failed: " + e.Message);
        }

        _host = null;
        _panel = null;
        _eraserSlider = null;
        _pencilSlider = null;
        _eraserLabel = null;
        _pencilLabel = null;
        _preview = null;
        _layer = null;
        ColorButtons.Clear();
        _dragging = false;
    }

    private static void OnPanelInput(InputEvent inputEvent)
    {
        if (_panel == null || !GodotObject.IsInstanceValid(_panel)) return;

        if (inputEvent is InputEventMouseButton button && button.ButtonIndex == MouseButton.Left)
        {
            _dragging = button.Pressed;
            if (_dragging)
                _dragOffset = button.GlobalPosition - _panel.GlobalPosition;
            return;
        }

        if (_dragging && inputEvent is InputEventMouseMotion motion)
        {
            var rootSize = _panel.GetViewportRect().Size;
            var panelSize = _panel.Size;
            var pos = motion.GlobalPosition - _dragOffset;
            pos.X = Mathf.Clamp(pos.X, 8f, Mathf.Max(8f, rootSize.X - panelSize.X - 8f));
            pos.Y = Mathf.Clamp(pos.Y, 8f, Mathf.Max(8f, rootSize.Y - panelSize.Y - 8f));

            _panel.AnchorLeft = 0f;
            _panel.AnchorRight = 0f;
            _panel.AnchorTop = 0f;
            _panel.AnchorBottom = 0f;
            _panel.OffsetLeft = pos.X;
            _panel.OffsetTop = pos.Y;
            _panel.OffsetRight = pos.X + panelSize.X;
            _panel.OffsetBottom = pos.Y + panelSize.Y;
        }
    }
}
