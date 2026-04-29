using System;
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace EraserMod;

public static class Bootstrap
{
    private const string HarmonyId = "com.kurah.sts2.erasermod";
    private static bool _initialized;

    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            Log("EraserMod loading...");
            Config.Load();
            Harmony.DEBUG = true;
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            int ours = 0;
            foreach (var m in harmony.GetPatchedMethods())
            {
                Log($"  patched: {m.DeclaringType?.FullName}.{m.Name}");
                ours++;
            }
            Log($"EraserMod ready. eraser={Config.EraserMultiplier:F2}, pencil={Config.PencilMultiplier:F2}, our patches={ours}");
        }
        catch (Exception e)
        {
            Log("EraserMod init failed: " + e);
        }
    }

    public static void Log(string msg)
    {
        var stamped = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        try
        {
            File.AppendAllText(Config.LogPath, stamped + Environment.NewLine);
        }
        catch { }

        if (_godotReady)
        {
            try { Godot.GD.Print("[EraserMod] " + msg); }
            catch { }
            try { LogOverlay.Append(stamped); }
            catch { }
        }
    }

    private static bool _godotReady;
    public static void MarkGodotReady() => _godotReady = true;
}
