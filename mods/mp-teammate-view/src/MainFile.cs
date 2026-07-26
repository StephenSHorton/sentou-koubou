using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace MpTeammateView;

/// <summary>
/// Combined multiplayer teammate UI: potions + hand cards next to the player list.
/// Based on BAKAOLC/OLC's MultiPlayerPotionView and ShowPlayerHandCards (MIT),
/// rewritten as one Harmony mod with more reliable hand attach.
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "MpTeammateView";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var harmony = new Harmony(ModId);
        harmony.PatchAll();
        Logger.Info(
            "MP Teammate View loaded — teammate potions + hand cards. " +
            "Disable Workshop MultiPlayerPotionView / ShowPlayerHandCards if both are enabled.");
    }
}
