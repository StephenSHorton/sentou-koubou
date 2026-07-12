using Godot;

namespace TradingPost;

/// <summary>
/// Loads Trading Post PNGs from beside the mod DLL (copied by the build target).
/// Prefer transparent cutouts so rest-site / menu chrome does not show black boxes.
/// </summary>
public static class TradeAssets
{
    private static readonly Dictionary<string, Texture2D> Cache = new();

    public static Texture2D? OptionTrade => Load("option_trade.png");
    public static Texture2D? MenuBanner => Load("menu_banner.png");
    public static Texture2D? IconGold => Load("icon_gold.png") ?? TryGame("res://images/packed/sprite_fonts/gold_icon.png");
    public static Texture2D? IconCard => Load("icon_card.png");
    public static Texture2D? IconTrade => Load("icon_trade.png") ?? OptionTrade;

    public static Texture2D? Load(string fileName)
    {
        if (Cache.TryGetValue(fileName, out Texture2D? cached) && GodotObject.IsInstanceValid(cached))
        {
            return cached;
        }
        try
        {
            string dir = Path.GetDirectoryName(typeof(MainFile).Assembly.Location)!;
            string path = Path.Combine(dir, fileName);
            if (!File.Exists(path))
            {
                MainFile.Logger.Warn($"Trading Post asset missing: {path}");
                return null;
            }
            Image image = Image.LoadFromFile(path);
            // Safety: if someone ships an opaque black-framed PNG again, punch it out.
            PunchNearBlackToAlpha(image);
            Texture2D tex = ImageTexture.CreateFromImage(image);
            Cache[fileName] = tex;
            return tex;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Trading Post asset load failed ({fileName}): {e.Message}");
            return null;
        }
    }

    private static Texture2D? TryGame(string resPath)
    {
        try
        {
            if (ResourceLoader.Exists(resPath))
            {
                return GD.Load<Texture2D>(resPath);
            }
        }
        catch
        {
            // ignore
        }
        return null;
    }

    /// <summary>
    /// Convert near-black fully-opaque pixels to transparent (fixes uncut rest icons).
    /// Leaves intentional black line art that sits on non-black neighbors alone.
    /// </summary>
    public static void PunchNearBlackToAlpha(Image image)
    {
        int w = image.GetWidth();
        int h = image.GetHeight();
        // Only run if the image has no useful transparency already.
        int transparent = 0;
        int samples = 0;
        for (int y = 0; y < h; y += Math.Max(1, h / 32))
        {
            for (int x = 0; x < w; x += Math.Max(1, w / 32))
            {
                samples++;
                if (image.GetPixel(x, y).A < 0.05f)
                {
                    transparent++;
                }
            }
        }
        if (samples > 0 && transparent / (float)samples > 0.05f)
        {
            return; // already cut
        }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = image.GetPixel(x, y);
                float maxc = Math.Max(c.R, Math.Max(c.G, c.B));
                float lum = 0.2126f * c.R + 0.7152f * c.G + 0.0722f * c.B;
                if (lum < 0.07f && maxc < 0.11f)
                {
                    c.A = 0f;
                    image.SetPixel(x, y, c);
                }
            }
        }
    }
}
