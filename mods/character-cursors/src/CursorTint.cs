using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace CharacterCursors;

/// <summary>Shared color resolve + local cursor image recolor.</summary>
public static class CursorTint
{
    /// <summary>Luminance below this is treated as outline (kept dark for contrast).</summary>
    public const float OutlineLumThreshold = 50f / 255f;

    private static readonly Vector2 DefaultHotSpot = new(14f, 5f);

    private static Color? _appliedColor;
    private static Image? _baseTilted;
    private static Image? _baseNotTilted;
    private static Image? _tintedTilted;
    private static Image? _tintedNotTilted;

    /// <summary>
    /// Character "primary" brand color: <see cref="CharacterModel.NameColor"/>.
    /// Falls back to <see cref="CharacterModel.MapDrawingColor"/> if name color is unusable.
    /// </summary>
    public static Color GetPrimaryColor(CharacterModel character)
    {
        var name = character.NameColor;
        if (name.A > 0.01f && (name.R + name.G + name.B) > 0.05f)
            return name;

        var map = character.MapDrawingColor;
        if (map.A > 0.01f && (map.R + map.G + map.B) > 0.05f)
            return map;

        return Colors.White;
    }

    public static Color? TryGetLocalPrimaryColor()
    {
        var player = TryGetLocalPlayer();
        return player?.Character == null ? null : GetPrimaryColor(player.Character);
    }

    public static Player? TryGetLocalPlayer()
    {
        try
        {
            var state = RunManager.Instance?.State;
            if (state?.Players == null)
                return null;
            return state.Players.FirstOrDefault(LocalContext.IsMe);
        }
        catch
        {
            return null;
        }
    }

    public static Player? TryGetPlayerByNetId(ulong netId)
    {
        try
        {
            var state = RunManager.Instance?.State;
            if (state?.Players == null)
                return null;
            return state.Players.FirstOrDefault(p => p.NetId == netId);
        }
        catch
        {
            return null;
        }
    }

    public static void ApplyLocalCursor()
    {
        var color = TryGetLocalPrimaryColor();
        if (color == null)
            return;

        var manager = NGame.Instance?.CursorManager;
        if (manager == null || !GodotObject.IsInstanceValid(manager))
            return;

        try
        {
            CaptureBaseImages(manager);
            if (_baseTilted == null || _baseNotTilted == null)
                return;

            if (!ColorsApproxEqual(_appliedColor, color.Value) || _tintedTilted == null || _tintedNotTilted == null)
            {
                _tintedTilted = RecolorImage(_baseTilted, color.Value);
                _tintedNotTilted = RecolorImage(_baseNotTilted, color.Value);
                _appliedColor = color.Value;
                var characterName = TryGetLocalPlayer()?.Character?.Id.ToString() ?? "?";
                MainFile.Logger.Info($"Local cursor tinted to {color.Value} ({characterName})");
            }

            manager.OverrideCursor(_tintedTilted, _tintedNotTilted, DefaultHotSpot);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Local cursor tint failed: {e.Message}");
        }
    }

    public static void ClearAppliedCache()
    {
        _appliedColor = null;
        _baseTilted = null;
        _baseNotTilted = null;
        _tintedTilted = null;
        _tintedNotTilted = null;
    }

    private static bool ColorsApproxEqual(Color? a, Color b)
    {
        if (a is not { } c)
            return false;
        return Mathf.IsEqualApprox(c.R, b.R)
               && Mathf.IsEqualApprox(c.G, b.G)
               && Mathf.IsEqualApprox(c.B, b.B)
               && Mathf.IsEqualApprox(c.A, b.A);
    }

    private static void CaptureBaseImages(NCursorManager manager)
    {
        // Publicized private exports — original untinted cursor art.
        if (_baseTilted != null && _baseNotTilted != null)
            return;

        var tilted = manager._cursorTilted;
        var notTilted = manager._cursorNotTilted;
        if (tilted == null || notTilted == null)
            return;

        _baseTilted = (Image)tilted.Duplicate();
        _baseNotTilted = (Image)notTilted.Duplicate();
    }

    /// <summary>
    /// Desaturate by luminance, multiply by tint; keep dark outline pixels dark for readability.
    /// </summary>
    public static Image RecolorImage(Image source, Color tint)
    {
        var img = (Image)source.Duplicate();
        if (img.GetFormat() != Image.Format.Rgba8)
            img.Convert(Image.Format.Rgba8);

        var size = img.GetSize();
        for (int y = 0; y < size.Y; y++)
        {
            for (int x = 0; x < size.X; x++)
            {
                var p = img.GetPixel(x, y);
                if (p.A < 0.05f)
                    continue;

                float lum = p.R * 0.299f + p.G * 0.587f + p.B * 0.114f;
                if (lum < OutlineLumThreshold)
                {
                    // Soft dark outline (not pure black) so it reads on all backgrounds.
                    img.SetPixel(x, y, new Color(0.12f, 0.12f, 0.12f, p.A));
                }
                else
                {
                    img.SetPixel(x, y, new Color(
                        Mathf.Clamp(lum * tint.R, 0f, 1f),
                        Mathf.Clamp(lum * tint.G, 0f, 1f),
                        Mathf.Clamp(lum * tint.B, 0f, 1f),
                        p.A));
                }
            }
        }

        return img;
    }
}
