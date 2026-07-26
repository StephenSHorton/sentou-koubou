using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace SharedCombatPositions;

/// <summary>
/// Vanilla always puts the local player first in the combat lineup
/// (<c>PositionPlayersAndPets</c> inserts IsMe at index 0). That makes each
/// peer see a different spatial layout. This mod sorts by lobby slot order
/// (RunState.Players index) so every client matches the host's view.
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
            "Shared Combat Positions loaded — multiplayer combat lineup uses lobby/host slot order " +
            "(not local-player-always-front). Enable on all peers for a consistent view.");
    }
}
