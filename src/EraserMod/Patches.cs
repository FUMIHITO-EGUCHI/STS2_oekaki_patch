using System;
using System.Reflection;
using Godot;
using HarmonyLib;
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
        var statesField = typeof(NMapDrawings).GetField("_drawingStates",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var netSvcField = typeof(NMapDrawings).GetField("_netService",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (statesField == null || netSvcField == null) { Bootstrap.Log("missing fields"); return; }

        var netSvc = netSvcField.GetValue(inst);
        if (netSvc == null) { Bootstrap.Log("netSvc null"); return; }

        var netIdProp = netSvc.GetType().GetProperty("NetId")
                        ?? netSvc.GetType().GetInterfaces()[0].GetProperty("NetId");
        ulong localNetId = (ulong)netIdProp.GetValue(netSvc);

        var states = (System.Collections.IEnumerable)statesField.GetValue(inst);
        foreach (var state in states)
        {
            var pidField = state.GetType().GetField("playerId");
            if (pidField == null) continue;
            ulong pid = (ulong)pidField.GetValue(state);
            if (pid != localNetId) continue;

            var modeField = state.GetType().GetField("drawingMode");
            var lineField = state.GetType().GetField("currentlyDrawingLine");
            var mode = modeField?.GetValue(state);
            var line = lineField?.GetValue(state) as Line2D;
            bool isErasing = mode != null && mode.ToString() == "Erasing";
            Bootstrap.Log($"local state: mode={mode} line={(line == null ? "null" : line.Width.ToString("F1"))}");
            if (line == null || !isErasing) return;

            float before = line.Width;
            line.Width = before * Config.WidthMultiplier;
            Bootstrap.Log($"Eraser line: {before:F1} -> {line.Width:F1} (x{Config.WidthMultiplier:F2})");
            return;
        }
        Bootstrap.Log("no local drawing state found");
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
    static void Postfix(bool isErasing) => Bootstrap.Log($"CreateLineForPlayer fired isErasing={isErasing}");
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
