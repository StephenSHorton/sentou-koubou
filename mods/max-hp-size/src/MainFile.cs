using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace MaxHpSize;

/// <summary>
/// Scales player combat sprites by max HP vs character starting HP.
/// Reimplementation of Workshop MaxHpSizeMod (BittersweetGirlJay).
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "MaxHpSize";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var harmony = new Harmony(ModId);
        harmony.PatchAll();
        Logger.Info(
            "Max HP Size loaded — player scale = 1 + (maxHp - startingHp) / startingHp " +
            "(min 0.25). Disable Workshop MaxHpSizeMod if both are enabled.");
    }
}
