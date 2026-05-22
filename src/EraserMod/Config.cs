using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace EraserMod;

public enum PaintTool
{
    Pencil,
    Eraser
}

public static class Config
{
    public const float Min = 0.5f;
    public const float Max = 12.0f;
    public const float Step = 0.5f;
    public const int CurrentSchema = 1;

    public static int Schema = CurrentSchema;
    public static float EraserMultiplier = 3.0f;
    public static float PencilMultiplier = 1.0f;
    public static string PencilColorHex;
    public static PaintTool SelectedTool = PaintTool.Pencil;
    public static bool ToolbarVisible = true;

    // Runtime-only: tracks local player's current drawing mode to avoid premature
    // DrawingState creation (which causes a black SubViewport to appear on the map).
    internal static DrawingMode LocalDrawingMode = DrawingMode.None;

    public static float WidthMultiplier
    {
        get => EraserMultiplier;
        set => EraserMultiplier = ClampStep(value);
    }

    private static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "MegaCrit", "SlayTheSpire2", "EraserMod");

    private static readonly string JsonConfigPath = Path.Combine(ConfigDir, "config.json");
    private static readonly string LegacyConfigPath = Path.Combine(ConfigDir, "config.txt");
    public static readonly string LogPath = Path.Combine(ConfigDir, "log.txt");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static void Load()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            if (File.Exists(JsonConfigPath))
            {
                var data = JsonSerializer.Deserialize<ConfigData>(File.ReadAllText(JsonConfigPath), JsonOptions);
                if (data != null) Apply(data);
                return;
            }

            if (File.Exists(LegacyConfigPath))
            {
                var s = File.ReadAllText(LegacyConfigPath).Trim();
                if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                    EraserMultiplier = ClampStep(v);
                Save();
                File.Delete(LegacyConfigPath);
            }
        }
        catch (Exception e)
        {
            Bootstrap.Log("Config.Load error: " + e.Message);
        }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            File.WriteAllText(JsonConfigPath, JsonSerializer.Serialize(ToData(), JsonOptions));
        }
        catch (Exception e)
        {
            Bootstrap.Log("Config.Save error: " + e.Message);
        }
    }

    public static void AdjustEraser(float delta)
    {
        EraserMultiplier = ClampStep(EraserMultiplier + delta);
        Save();
    }

    public static void AdjustPencil(float delta)
    {
        PencilMultiplier = ClampStep(PencilMultiplier + delta);
        Save();
    }

    public static void Adjust(float delta) => AdjustEraser(delta);

    public static float ClampStep(float value)
    {
        return MathF.Round(Math.Clamp(value, Min, Max) / Step) * Step;
    }

    private static void Apply(ConfigData data)
    {
        Schema = data.Schema <= 0 ? CurrentSchema : data.Schema;
        EraserMultiplier = ClampStep(data.EraserMultiplier <= 0f ? 3.0f : data.EraserMultiplier);
        PencilMultiplier = ClampStep(data.PencilMultiplier <= 0f ? 1.0f : data.PencilMultiplier);
        PencilColorHex = NormalizeHex(data.PencilColorHex);
        SelectedTool = ParseTool(data.SelectedTool);
        ToolbarVisible = data.ToolbarVisible;
    }

    private static ConfigData ToData() => new()
    {
        Schema = CurrentSchema,
        EraserMultiplier = EraserMultiplier,
        PencilMultiplier = PencilMultiplier,
        PencilColorHex = NormalizeHex(PencilColorHex),
        SelectedTool = SelectedTool.ToString(),
        ToolbarVisible = ToolbarVisible
    };

    public static string NormalizeHex(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var s = value.Trim();
        if (!s.StartsWith("#", StringComparison.Ordinal)) s = "#" + s;
        if (s.Length != 7) return null;
        for (int i = 1; i < s.Length; i++)
        {
            if (!Uri.IsHexDigit(s[i])) return null;
        }
        return s.ToUpperInvariant();
    }

    private sealed class ConfigData
    {
        public int Schema { get; set; } = CurrentSchema;
        public float EraserMultiplier { get; set; } = 3.0f;
        public float PencilMultiplier { get; set; } = 1.0f;
        public string PencilColorHex { get; set; }
        public string SelectedTool { get; set; } = PaintTool.Pencil.ToString();
        public bool ToolbarVisible { get; set; } = true;
    }

    private static PaintTool ParseTool(string value)
    {
        return value == nameof(PaintTool.Eraser) ? PaintTool.Eraser : PaintTool.Pencil;
    }
}
