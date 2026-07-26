using Godot;

namespace BattleDraw;

/// <summary>Loads toolbar PNGs from beside the mod DLL.</summary>
public static class DrawAssets
{
    private static readonly Dictionary<string, Texture2D> Cache = new();

    public static Texture2D? PanelTools => Load("panel_tools.png", punchBlack: false);
    public static Texture2D? IconBrush => Load("icon_brush.png", punchBlack: false);
    public static Texture2D? IconEraser => Load("icon_eraser.png", punchBlack: false);
    public static Texture2D? IconClear => Load("icon_clear.png", punchBlack: false);
    public static Texture2D? IconTab => Load("icon_tab.png", punchBlack: false);
    public static Texture2D? ModImage => Load("mod_image.png", punchBlack: false);

    public static Texture2D? Load(string fileName, bool punchBlack = true)
    {
        string key = punchBlack ? fileName : fileName + "#raw";
        if (Cache.TryGetValue(key, out Texture2D? cached) && GodotObject.IsInstanceValid(cached))
            return cached;

        try
        {
            string dir = Path.GetDirectoryName(typeof(MainFile).Assembly.Location)!;
            string path = Path.Combine(dir, fileName);
            if (!File.Exists(path))
            {
                MainFile.Logger.Warn($"Battle Draw asset missing: {path}");
                return null;
            }

            Image image = Image.LoadFromFile(path);
            if (punchBlack)
                PunchNearBlackToAlpha(image);
            Texture2D tex = ImageTexture.CreateFromImage(image);
            Cache[key] = tex;
            return tex;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Battle Draw asset load failed ({fileName}): {e.Message}");
            return null;
        }
    }

    public static StyleBoxTexture? MakeNineSlice(Texture2D? tex, float margin = 36f, float content = 20f)
    {
        if (tex == null)
            return null;
        return new StyleBoxTexture
        {
            Texture = tex,
            TextureMarginLeft = margin,
            TextureMarginRight = margin,
            TextureMarginTop = margin,
            TextureMarginBottom = margin,
            ContentMarginLeft = content,
            ContentMarginRight = content,
            ContentMarginTop = content * 0.65f,
            ContentMarginBottom = content * 0.65f,
        };
    }

    private static void PunchNearBlackToAlpha(Image image)
    {
        int w = image.GetWidth();
        int h = image.GetHeight();
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = image.GetPixel(x, y);
                if (c.R < 0.06f && c.G < 0.06f && c.B < 0.06f)
                    image.SetPixel(x, y, new Color(c.R, c.G, c.B, 0f));
            }
        }
    }
}
