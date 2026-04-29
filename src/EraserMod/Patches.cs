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
        Bootstrap.Log("BeginLineLocal postfix fired");
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
    static void Postfix() => Bootstrap.Log("BeginLine postfix fired");
}

[HarmonyPatch(typeof(NMapDrawings), "CreateLineForPlayer")]
public static class NMapDrawings_CreateLineForPlayer_Patch
{
    static void Postfix(NMapDrawings __instance, Line2D __result, Player player, bool isErasing)
    {
        Bootstrap.Log($"CreateLineForPlayer fired isErasing={isErasing}");
        if (__result == null || player == null) return;
        if (player.NetId != MapReflection.GetLocalNetId(__instance)) return;

        if (isErasing)
        {
            var eraseColor = new Color(1f, 1f, 1f, __result.DefaultColor.A);
            __result.DefaultColor = eraseColor;
            Bootstrap.Log($"Eraser color forced to white alpha={eraseColor.A:F2}");
            return;
        }

        float before = __result.Width;
        __result.Width = before * Config.PencilMultiplier;
        if (ColorUtil.TryParseHex(Config.PencilColorHex, out var color))
        {
            color.A = __result.DefaultColor.A;
            __result.DefaultColor = color;
        }
        Bootstrap.Log($"Pencil line: {before:F1} -> {__result.Width:F1} (x{Config.PencilMultiplier:F2})");
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
