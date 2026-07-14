using Godot;

namespace CardRanks;

/// <summary>Loads PNG cutouts shipped beside the DLL (no .pck required).</summary>
public static class RankAssets
{
    private static string? _modDir;
    private static Texture2D? _combineIcon;
    private static Texture2D? _rank2;
    private static Texture2D? _rank3;

    public static string ModDir
    {
        get
        {
            if (_modDir != null)
                return _modDir;
            string? loc = typeof(MainFile).Assembly.Location;
            _modDir = string.IsNullOrEmpty(loc)
                ? AppContext.BaseDirectory
                : Path.GetDirectoryName(loc) ?? AppContext.BaseDirectory;
            return _modDir;
        }
    }

    public static Texture2D? CombineIcon => _combineIcon ??= LoadPng("combine_rest_site.png");

    public static Texture2D? Rank2Icon => _rank2 ??= LoadPng("rank2.png");

    public static Texture2D? Rank3Icon => _rank3 ??= LoadPng("rank3.png");

    private static Texture2D? LoadPng(string fileName)
    {
        string path = Path.Combine(ModDir, fileName);
        if (!File.Exists(path))
        {
            // Also try assets/ subfolder (dev builds)
            path = Path.Combine(ModDir, "assets", fileName);
            if (!File.Exists(path))
            {
                MainFile.Logger.Warn($"Missing asset: {fileName} under {ModDir}");
                return null;
            }
        }

        Image image = new();
        Error err = image.Load(path);
        if (err != Error.Ok)
        {
            MainFile.Logger.Warn($"Failed to load {path}: {err}");
            return null;
        }

        return ImageTexture.CreateFromImage(image);
    }
}
