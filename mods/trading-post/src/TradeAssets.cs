using Godot;

namespace TradingPost;

/// <summary>
/// Loads Trading Post PNGs from beside the mod DLL (copied by the build target).
/// Campfire option art should be a full-bleed plate (like Rest/Smith/Mend).
/// Menu uses painted panel/button plates for STS2 chrome.
/// </summary>
public static class TradeAssets
{
    private static readonly Dictionary<string, Texture2D> Cache = new();

    public static Texture2D? OptionTrade => Load("option_trade.png", punchBlack: false);
    public static Texture2D? MenuBanner => Load("menu_banner.png", punchBlack: false);
    public static Texture2D? MenuPanel => Load("menu_panel.png", punchBlack: false);
    public static Texture2D? BtnPlate => Load("btn_plate.png", punchBlack: false);
    public static Texture2D? IconGold => Load("icon_gold.png") ?? TryGame("res://images/packed/sprite_fonts/gold_icon.png");
    public static Texture2D? IconCard => Load("icon_card.png");
    public static Texture2D? IconTrade => Load("icon_trade.png") ?? OptionTrade;

    public static Texture2D? Load(string fileName, bool punchBlack = true)
    {
        string key = punchBlack ? fileName : fileName + "#nopunch";
        if (Cache.TryGetValue(key, out Texture2D? cached) && GodotObject.IsInstanceValid(cached))
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
            if (punchBlack)
            {
                PunchNearBlackToAlpha(image);
            }
            Texture2D tex = ImageTexture.CreateFromImage(image);
            Cache[key] = tex;
            return tex;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Trading Post asset load failed ({fileName}): {e.Message}");
            return null;
        }
    }

    /// <summary>9-slice stylebox from a painted plate texture.</summary>
    public static StyleBoxTexture? MakeNineSlice(Texture2D? tex, float margin = 28f, float content = 18f)
    {
        if (tex == null)
        {
            return null;
        }
        return new StyleBoxTexture
        {
            Texture = tex,
            TextureMarginLeft = margin,
            TextureMarginRight = margin,
            TextureMarginTop = margin,
            TextureMarginBottom = margin,
            ContentMarginLeft = content,
            ContentMarginRight = content,
            ContentMarginTop = content * 0.7f,
            ContentMarginBottom = content * 0.7f,
        };
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
    /// Convert near-black fully-opaque pixels to transparent (legacy uncut icons only).
    /// Skipped for full-bleed plates that intentionally use dark stone edges.
    /// </summary>
    public static void PunchNearBlackToAlpha(Image image)
    {
        int w = image.GetWidth();
        int h = image.GetHeight();
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
            return;
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
