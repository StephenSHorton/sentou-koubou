using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace PingRage;

/// <summary>
/// Spice up the multiplayer end-turn Ping button: shuffled funny lines,
/// rage-scaled bubble size, and increasingly chaotic wiggle when mashed.
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "PingRage";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var harmony = new Harmony(ModId);
        harmony.PatchAll();
        Logger.Info(
            "Ping Rage loaded — mash Ping for bigger bubbles; funny lines + rage sync in multiplayer.");
    }
}
