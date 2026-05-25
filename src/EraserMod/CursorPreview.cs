using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace EraserMod;

public partial class CursorPreview : Node2D
{
    private const int SegmentCount = 96;
    private NMapDrawings _host;
    private Polygon2D _fill;
    private Line2D _outerRing;
    private Line2D _innerRing;
    private PaintTool _tool = PaintTool.Pencil;
    private Color _previewColor = new(1f, 1f, 1f, 0.9f);
    private float _radius = 6f;
    private float _drawScale = 1f;
    private Vector2 _viewportSize;
    private float _lastNodeRadius = -1f;
    private Color _lastNodeColor = new(-1f, -1f, -1f, -1f);
    private bool _loggedVisible;
    private bool _loggedHidden;
    private bool _loggedRect;

    public void Attach(NMapDrawings host)
    {
        _host = host;
        Name = "EraserModCursorPreview";
        ZIndex = 4096;
        ProcessMode = ProcessModeEnum.Always;
        BuildNodes();
        SetProcess(true);
        Bootstrap.Log("cursor preview attached");
    }

    public void UpdateHost(NMapDrawings host) => _host = host;

    public override void _Process(double delta) => UpdateFrame();

    public void UpdateFrame()
    {
        if (_host == null || !GodotObject.IsInstanceValid(_host))
        {
            QueueFree();
            return;
        }

        // Use cached mode from SetDrawingModeLocal_Patch — avoids calling GetLocalState
        // (which triggers GetDrawingStateForPlayer) before the player enters drawing mode.
        // Premature DrawingState creation shows a black SubViewport over the map.
        var mode = Config.LocalDrawingMode;
        _tool = mode == DrawingMode.Erasing ? PaintTool.Eraser : PaintTool.Pencil;

        if (!IsDrawingTool(mode))
        {
            Visible = false;
            return;
        }

        _viewportSize = GetViewportRect().Size;
        Position = GetLocalMousePositionSafe();
        _drawScale = MapReflection.GetDrawScreenScale(_host);
        Visible = IsHostActive() && IsOverDrawArea(Position);
        if (!Visible)
        {
            if (!_loggedHidden)
            {
                _loggedHidden = true;
                Bootstrap.Log("cursor preview hidden: host inactive");
            }
            return;
        }

        UpdatePreviewStyle();
        UpdateNodesIfNeeded();
        if (!_loggedVisible)
        {
            _loggedVisible = true;
            Bootstrap.Log($"cursor preview visible size={_viewportSize.X:F0}x{_viewportSize.Y:F0} pos={Position.X:F0},{Position.Y:F0} tool={_tool} radius={_radius:F1} scale={_drawScale:F2}");
        }
    }

    private void UpdatePreviewStyle()
    {
        if (_tool == PaintTool.Eraser)
        {
            _radius = 6f * Config.EraserMultiplier * _drawScale;
            _previewColor = new Color(1f, 1f, 1f, 0.9f);
            return;
        }

        _radius = 2f * Config.PencilMultiplier * _drawScale;
        if (ColorUtil.TryParseHex(Config.PencilColorHex, out var color))
        {
            color.A = 0.9f;
            _previewColor = color;
            return;
        }

        _previewColor = MapReflection.TryGetLocalColor(_host, out var localColor)
            ? new Color(localColor.R, localColor.G, localColor.B, 0.9f)
            : new Color(1f, 1f, 1f, 0.9f);
    }

    private bool IsHostActive()
    {
        try
        {
            return _host != null && GodotObject.IsInstanceValid(_host) && _host.IsInsideTree();
        }
        catch
        {
            return false;
        }
    }

    private bool IsOverDrawArea(Vector2 position)
    {
        if (!MapReflection.TryGetDrawScreenRect(_host, out var rect))
            return true;

        if (!_loggedRect)
        {
            _loggedRect = true;
            Bootstrap.Log($"cursor preview draw rect pos={rect.Position.X:F0},{rect.Position.Y:F0} size={rect.Size.X:F0}x{rect.Size.Y:F0}");
        }
        return rect.HasPoint(position);
    }

    private static bool IsDrawingTool(DrawingMode mode)
    {
        return mode == DrawingMode.Drawing || mode == DrawingMode.Erasing;
    }

    private Vector2 GetLocalMousePositionSafe()
    {
        var viewportPos = GetViewport().GetMousePosition();
        if (viewportPos.X >= 0f && viewportPos.Y >= 0f && viewportPos.X <= _viewportSize.X && viewportPos.Y <= _viewportSize.Y)
            return viewportPos;

        try
        {
            var screenPos = DisplayServer.MouseGetPosition();
            var windowPos = DisplayServer.WindowGetPosition();
            var local = new Vector2(screenPos.X - windowPos.X, screenPos.Y - windowPos.Y);
            if (local.X >= 0f && local.Y >= 0f && local.X <= _viewportSize.X && local.Y <= _viewportSize.Y)
                return local;
        }
        catch { }

        return viewportPos.Clamp(Vector2.Zero, _viewportSize);
    }

    private void BuildNodes()
    {
        if (_fill != null && GodotObject.IsInstanceValid(_fill)) return;

        _fill = new Polygon2D { Name = "Fill", ZIndex = 0 };
        _outerRing = new Line2D { Name = "OuterRing", ZIndex = 1, Width = 2.0f, Closed = true };
        _innerRing = new Line2D { Name = "InnerRing", ZIndex = 2, Width = 3.0f, Closed = true };
        AddChild(_fill);
        AddChild(_outerRing);
        AddChild(_innerRing);
    }

    private void UpdateNodesIfNeeded()
    {
        if (Mathf.IsEqualApprox(_radius, _lastNodeRadius) && _previewColor.IsEqualApprox(_lastNodeColor))
            return;

        BuildNodes();
        var fillPoints = new Vector2[SegmentCount];
        var ringPoints = new Vector2[SegmentCount];
        for (var i = 0; i < SegmentCount; i++)
        {
            var angle = Mathf.Tau * i / SegmentCount;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            fillPoints[i] = dir * _radius;
            ringPoints[i] = dir * (_radius + 2f);
        }

        _fill.Polygon = fillPoints;
        _fill.Color = new Color(_previewColor.R, _previewColor.G, _previewColor.B, 0.18f);
        _outerRing.Points = ringPoints;
        _outerRing.DefaultColor = new Color(0f, 0f, 0f, 0.75f);
        _innerRing.Points = fillPoints;
        _innerRing.DefaultColor = _previewColor;
        _lastNodeRadius = _radius;
        _lastNodeColor = _previewColor;
    }
}
