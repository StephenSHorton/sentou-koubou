using Godot;

namespace BattleBall;

/// <summary>Loads Battle Ball PNGs from beside the mod DLL (copied on build).</summary>
public static class BallAssets
{
    private static Texture2D? _hoop;
    private static bool _hoopTried;
    private static Texture2D? _hoopFront;
    private static bool _hoopFrontTried;
    private static Texture2D? _ball;
    private static bool _ballTried;

    /// <summary>Full hoop (backboard + rim + net). Drawn behind the ball.</summary>
    public static Texture2D? Hoop
    {
        get
        {
            if (_hoopTried)
                return _hoop;
            _hoopTried = true;
            _hoop = Load("hoop.png");
            return _hoop;
        }
    }

    /// <summary>
    /// Front lip of the rim (camera-facing cutout). Drawn in front of the ball so the
    /// ball can appear to pass through the hoop / net.
    /// </summary>
    public static Texture2D? HoopFront
    {
        get
        {
            if (_hoopFrontTried)
                return _hoopFront;
            _hoopFrontTried = true;
            _hoopFront = Load("hoop_front.png");
            return _hoopFront;
        }
    }

    /// <summary>Orange basketball (file texture — same load path as hoop.png).</summary>
    public static Texture2D? Ball
    {
        get
        {
            if (_ballTried)
                return _ball;
            _ballTried = true;
            _ball = Load("ball.png");
            return _ball;
        }
    }

    public static Texture2D? Load(string fileName)
    {
        try
        {
            string dir = Path.GetDirectoryName(typeof(MainFile).Assembly.Location)!;
            string path = Path.Combine(dir, fileName);
            if (!File.Exists(path))
            {
                MainFile.Logger.Warn($"Battle Ball asset missing: {path}");
                return null;
            }
            Image image = Image.LoadFromFile(path);
            return ImageTexture.CreateFromImage(image);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Battle Ball asset load failed ({fileName}): {e.Message}");
            return null;
        }
    }
}
