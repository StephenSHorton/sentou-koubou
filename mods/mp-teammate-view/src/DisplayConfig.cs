using Godot;

namespace MpTeammateView;

/// <summary>Fixed layout knobs (settings UI can come later).</summary>
public static class DisplayConfig
{
    public const float PotionSlotPx = 24f;
    public const float PotionSeparation = 2f;
    public const float PotionYNudge = -2f;

    public const float MiniCardScale = 0.28f;
    public const float CardSpacing = 4f;
    public const float HandAutoGap = 8f;

    // Approximate full card size used for mini scaling
    public static Vector2 FullCardSize => new(250f, 350f);

    public static Vector2 ScaledCardSize => FullCardSize * MiniCardScale;

    public static float HandContentWidth(int count)
    {
        if (count <= 0)
            return 0f;
        float w = ScaledCardSize.X;
        return count * w + (count - 1) * CardSpacing;
    }

    public static float PotionContentWidth(int count)
    {
        if (count <= 0)
            return 0f;
        return count * PotionSlotPx + (count - 1) * PotionSeparation;
    }
}
