using System;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace EraserMod;

internal static class MapReflection
{
    private static readonly FieldInfo NetServiceField = typeof(NMapDrawings).GetField("_netService", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo PlayerCollectionField = typeof(NMapDrawings).GetField("_playerCollection", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo GetDrawingStateForPlayerMethod = typeof(NMapDrawings).GetMethod("GetDrawingStateForPlayer", BindingFlags.NonPublic | BindingFlags.Instance);

    public static ulong GetLocalNetId(NMapDrawings host)
        => (NetServiceField?.GetValue(host) as INetGameService)?.NetId ?? 0;

    public static object GetLocalState(NMapDrawings host)
    {
        var id = GetLocalNetId(host);
        return id == 0 ? null : GetDrawingStateForPlayerMethod?.Invoke(host, new object[] { id });
    }

    public static object GetStateForNetId(NMapDrawings host, ulong netId)
        => netId == 0 ? null : GetDrawingStateForPlayerMethod?.Invoke(host, new object[] { netId });

    public static ulong GetStatePlayerId(object state)
    {
        var val = state?.GetType().GetField("playerId")?.GetValue(state);
        return val is ulong id ? id : 0ul;
    }

    public static SubViewport GetDrawViewport(object state)
    {
        return state?.GetType().GetField("drawViewport")?.GetValue(state) as SubViewport;
    }

    public static Line2D GetCurrentLine(object state)
    {
        return state?.GetType().GetField("currentlyDrawingLine")?.GetValue(state) as Line2D;
    }

    public static DrawingMode GetCurrentMode(object state)
    {
        if (state == null) return DrawingMode.None;
        var overrideValue = state.GetType().GetField("overrideDrawingMode")?.GetValue(state);
        if (overrideValue is DrawingMode overrideMode) return overrideMode;
        var modeValue = state.GetType().GetField("drawingMode")?.GetValue(state);
        return modeValue is DrawingMode mode ? mode : DrawingMode.None;
    }

    // True while the local player is mid-stroke (currentlyDrawingLine is non-null).
    public static bool IsCurrentlyDrawing(NMapDrawings host)
    {
        return GetCurrentLine(GetLocalState(host)) != null;
    }

    public static bool TryGetLocalColor(NMapDrawings host, out Color color)
    {
        color = new Color(1f, 1f, 1f, 1f);
        try
        {
            var players = PlayerCollectionField?.GetValue(host);
            var method = players?.GetType().GetMethod("GetPlayer", new[] { typeof(ulong) });
            var player = method?.Invoke(players, new object[] { GetLocalNetId(host) }) as Player;
            if (player == null) return false;
            color = player.Character.MapDrawingColor;
            return true;
        }
        catch (Exception e)
        {
            Bootstrap.Log("local color lookup failed: " + e.Message);
            return false;
        }
    }

    public static float GetDrawScreenScale(NMapDrawings host)
    {
        try
        {
            var viewport = GetDrawViewport(GetLocalState(host));
            if (viewport == null) return 1f;

            var parent = viewport.GetParent();
            if (parent is Control control)
            {
                var viewportSize = (Vector2)viewport.Size;
                var rectSize = control.GetGlobalRect().Size;
                if (viewportSize.X > 0f && viewportSize.Y > 0f && rectSize.X > 0f && rectSize.Y > 0f)
                    return (rectSize.X / viewportSize.X + rectSize.Y / viewportSize.Y) * 0.5f;
            }

            if (parent is CanvasItem item)
            {
                var scale = item.GetGlobalTransformWithCanvas().Scale;
                return Mathf.Max(0.01f, (Mathf.Abs(scale.X) + Mathf.Abs(scale.Y)) * 0.5f);
            }
        }
        catch (Exception e)
        {
            Bootstrap.Log("draw scale lookup failed: " + e.Message);
        }

        return 1f;
    }

    public static bool TryGetDrawScreenRect(NMapDrawings host, out Rect2 rect)
    {
        rect = default;
        try
        {
            if (host != null && GodotObject.IsInstanceValid(host))
            {
                rect = ((Control)host).GetGlobalRect();
                if (rect.Size.X > 0f && rect.Size.Y > 0f)
                    return true;
            }

            var viewport = GetDrawViewport(GetLocalState(host));
            if (viewport == null) return false;

            var parent = viewport.GetParent();
            if (parent is Control control)
            {
                rect = control.GetGlobalRect();
                return rect.Size.X > 0f && rect.Size.Y > 0f;
            }

            if (parent is CanvasItem item)
            {
                var viewportSize = (Vector2)viewport.Size;
                var transform = item.GetGlobalTransformWithCanvas();
                rect = new Rect2(transform.Origin, viewportSize * transform.Scale.Abs());
                return rect.Size.X > 0f && rect.Size.Y > 0f;
            }
        }
        catch (Exception e)
        {
            Bootstrap.Log("draw rect lookup failed: " + e.Message);
        }

        return false;
    }
}
