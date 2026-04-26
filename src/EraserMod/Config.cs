using System;
using System.Globalization;
using System.IO;

namespace EraserMod;

public static class Config
{
    public static float WidthMultiplier = 3.0f;
    public const float Min = 0.5f;
    public const float Max = 12.0f;
    public const float Step = 0.5f;

    private static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "MegaCrit", "SlayTheSpire2", "EraserMod");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.txt");
    public static readonly string LogPath = Path.Combine(ConfigDir, "log.txt");

    public static void Load()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            if (File.Exists(ConfigPath))
            {
                var s = File.ReadAllText(ConfigPath).Trim();
                if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                    WidthMultiplier = Math.Clamp(v, Min, Max);
            }
        }
        catch { }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            File.WriteAllText(ConfigPath,
                WidthMultiplier.ToString("F2", CultureInfo.InvariantCulture));
        }
        catch { }
    }

    public static void Adjust(float delta)
    {
        WidthMultiplier = MathF.Round(Math.Clamp(WidthMultiplier + delta, Min, Max) / Step) * Step;
        Save();
    }
}
