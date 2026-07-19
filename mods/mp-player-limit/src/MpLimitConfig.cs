using BaseLib.Config;

namespace MpPlayerLimit;

/// <summary>Shared settings exposed in BaseLib's mod config UI.</summary>
public sealed class MpLimitConfig : SimpleModConfig
{
    /// <summary>Vanilla multiplayer host capacity is 4. We raise host + lobby to this.</summary>
    public static int MaxPlayers { get; set; } = 16;

    public const int VanillaMax = 4;
    public const int MinAllowed = 2;
    public const int MaxAllowed = 16;

    /// <summary>Clamped capacity used by patches.</summary>
    public static int ClampedMax => Math.Clamp(MaxPlayers, MinAllowed, MaxAllowed);

    /// <summary>
    /// Replace vanilla's multiplayer default of 4 with our cap.
    /// Leaves 1 (singleplayer) and -1 (client lobby) alone.
    /// </summary>
    public static int RewriteCapacity(int vanilla)
    {
        if (vanilla == VanillaMax)
            return ClampedMax;
        return vanilla;
    }
}
