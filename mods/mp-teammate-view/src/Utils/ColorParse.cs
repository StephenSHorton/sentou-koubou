using Godot;

namespace MpTeammateView.Utils;

internal static class ColorParse
{
    public static bool TryParseHexColor(string? text, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();
        if (!trimmed.StartsWith('#'))
            trimmed = $"#{trimmed}";

        var hex = trimmed[1..];
        if (hex.Length is not (3 or 4 or 6 or 8) || hex.Any(c => !Uri.IsHexDigit(c)))
            return false;
        if (hex.Length is 3 or 4)
            hex = string.Concat(hex.Select(c => new string(c, 2)));
        if (hex.Length == 6)
            hex += "FF";

        color = new(
            Convert.ToByte(hex[..2], 16) / 255f,
            Convert.ToByte(hex[2..4], 16) / 255f,
            Convert.ToByte(hex[4..6], 16) / 255f,
            Convert.ToByte(hex[6..8], 16) / 255f);
        return true;
    }

    public static Color DefaultHighlight(string? settingsHex)
    {
        return TryParseHexColor(settingsHex, out var parsed)
            ? parsed
            : new(1f, 215f / 255f, 64f / 255f);
    }
}
