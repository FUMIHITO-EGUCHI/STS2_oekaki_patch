using System;
using System.Globalization;
using Godot;

namespace EraserMod;

public static class ColorUtil
{
    public static bool TryParseHex(string hex, out Color color)
    {
        color = default;
        hex = Config.NormalizeHex(hex);
        if (hex == null) return false;

        try
        {
            int r = int.Parse(hex.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int g = int.Parse(hex.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int b = int.Parse(hex.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            color = new Color(r / 255f, g / 255f, b / 255f, 1f);
            return true;
        }
        catch (Exception e)
        {
            Bootstrap.Log("invalid pencil color: " + e.Message);
            return false;
        }
    }
}
