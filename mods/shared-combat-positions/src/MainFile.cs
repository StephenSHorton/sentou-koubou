using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace SharedCombatPositions;

/// <summary>
/// Multiplayer combat visual QoL:
/// <list type="bullet">
/// <item>Lineup uses lobby/host slot order (not local-player-always-front).</item>
/// <item>Teammate HP / block / power icons stay visible without hover.</item>
/// <item>Ally state UI and orb slots draw above overlapping character sprites.</item>
/// </list>
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "SharedCombatPositions";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var harmony = new Harmony(ModId);
        harmony.PatchAll();
        Logger.Info(
            "Shared Combat Positions loaded — host-order lineup + always-visible teammate HP/status " +
            "(combat only; ally bars/orbs drawn above creature sprites; cleared on end so bars don't leak onto the map). " +
            "Enable on all peers for a consistent view.");
    }
}
