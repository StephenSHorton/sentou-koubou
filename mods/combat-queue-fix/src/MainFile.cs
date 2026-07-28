using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace CombatQueueFix;

/// <summary>
/// Vanilla MP race: a buffered map vote can enqueue as NonCombat after combat starts.
/// ActionQueueSet skips NonCombat during combat but never dequeues it, so PlayCard/EndTurn
/// for that player stall forever. Cancel/drop those heads so combat actions can run.
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "CombatQueueFix";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var harmony = new Harmony(ModId);
        harmony.PatchAll();
        Logger.Info(
            "Combat Queue Fix loaded — cancels NonCombat map-vote heads while in combat " +
            "so card plays cannot softlock behind VoteForMapCoordAction.");
    }
}
