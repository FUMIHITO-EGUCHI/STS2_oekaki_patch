using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace EraserMod;

[HarmonyPatch(typeof(NMapDrawings), "BeginLineLocal")]
public static class NMapDrawings_BeginLineLocal_Patch
{
    static void Postfix(NMapDrawings __instance)
    {
        try
        {
            HotkeyHandler.AttachOnce(__instance);
            ApplyEraserWidth(__instance);
        }
        catch (Exception e)
        {
            Bootstrap.Log("BeginLineLocal postfix error: " + e);
        }
    }

    internal static void ApplyEraserWidth(NMapDrawings inst)
    {
        // Reach into private _drawingStates list and find the local player's state.
        var state = MapReflection.GetLocalState(inst);
        var line = MapReflection.GetCurrentLine(state);
        if (line == null || MapReflection.GetCurrentMode(state) != DrawingMode.Erasing) return;

        float before = line.Width;
        line.Width = before * Config.EraserMultiplier;
        Bootstrap.Log($"Eraser line: {before:F1} -> {line.Width:F1} (x{Config.EraserMultiplier:F2})");
    }
}

[HarmonyPatch(typeof(NMapDrawings), "BeginLine")]
public static class NMapDrawings_BeginLine_Patch
{
    // Runs before the line is sent to peers via QueueOrSendEvent — good time to announce style.
    // `state` is bound by name to BeginLine's first parameter (the private DrawingState).
    // Using `object` avoids referencing the private type while staying robust against
    // game updates that reorder parameters (vs positional `__0` binding).
    static void Prefix(NMapDrawings __instance, object state)
    {
        try
        {
            if (MapReflection.GetStatePlayerId(state) == MapReflection.GetLocalNetId(__instance))
                NetSync.SendStyleAnnounce();
        }
        catch (Exception e) { Bootstrap.Log("BeginLine prefix err: " + e.Message); }
    }
}

[HarmonyPatch(typeof(NMapDrawings), "CreateLineForPlayer")]
public static class NMapDrawings_CreateLineForPlayer_Patch
{
    static void Postfix(NMapDrawings __instance, Line2D __result, Player player, bool isErasing)
    {
        if (__result == null || player == null) return;

        bool isLocal = player.NetId == MapReflection.GetLocalNetId(__instance);
        if (isLocal)
            ApplyLocalStyle(__result, isErasing);
        else
            ApplyPeerStyle(__result, player.NetId, isErasing);
    }

    private static void ApplyLocalStyle(Line2D line, bool isErasing)
    {
        if (isErasing)
        {
            line.DefaultColor = new Color(1f, 1f, 1f, line.DefaultColor.A);
            return;
        }

        float before = line.Width;
        line.Width = before * Config.PencilMultiplier;
        if (ColorUtil.TryParseHex(Config.PencilColorHex, out var color))
        {
            color.A = line.DefaultColor.A;
            line.DefaultColor = color;
        }
        Bootstrap.Log($"Pencil line: {before:F1} -> {line.Width:F1} (x{Config.PencilMultiplier:F2})");
    }

    private static void ApplyPeerStyle(Line2D line, ulong peerId, bool isErasing)
    {
        if (!PeerStyleCache.TryGetStyle(peerId, out var style)) return;

        float before = line.Width;
        if (isErasing)
        {
            var eraseColor = new Color(1f, 1f, 1f, line.DefaultColor.A);
            line.DefaultColor = eraseColor;
            line.Width = before * style.EraserMultiplier;
            Bootstrap.Log($"Peer {peerId} eraser: {before:F1} -> {line.Width:F1}");
        }
        else
        {
            line.Width = before * style.PencilMultiplier;
            if (ColorUtil.TryParseHex(style.ColorHex, out var color))
            {
                color.A = line.DefaultColor.A;
                line.DefaultColor = color;
            }
            Bootstrap.Log($"Peer {peerId} pencil: {before:F1} -> {line.Width:F1}");
        }
    }
}

[HarmonyPatch(typeof(NMapDrawings), "_Ready")]
public static class NMapDrawings_Ready_Patch
{
    static void Postfix(NMapDrawings __instance)
    {
        Bootstrap.Log("NMapDrawings._Ready postfix fired");
        try { HotkeyHandler.AttachOnce(__instance); }
        catch (Exception e) { Bootstrap.Log("ready attach err: " + e); }
    }
}

[HarmonyPatch(typeof(NMapDrawings), "SetDrawingModeLocal")]
public static class NMapDrawings_SetDrawingModeLocal_Patch
{
    static void Postfix(DrawingMode drawingMode)
    {
        Config.SelectedTool = drawingMode == DrawingMode.Erasing ? PaintTool.Eraser : PaintTool.Pencil;
        Config.Save();
        Toolbar.Refresh();
    }
}
