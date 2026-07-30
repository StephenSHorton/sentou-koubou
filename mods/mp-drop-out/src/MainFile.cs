using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace MpDropOut;

/// <summary>
/// Vanilla MP: disconnect only removes peer input / lobby connection state.
/// Combat still waits for every <see cref="MegaCrit.Sts2.Core.Entities.Players.Player"/> to
/// end turn, map/event/act/treasure still wait for every slot — leavers softlock the run.
/// This mod treats disconnected peers as non-participants and advances their gates.
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "MpDropOut";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var harmony = new Harmony(ModId);
        harmony.PatchAll();
        Logger.Info(
            "MP Drop Out loaded — disconnected peers no longer block end-turn, " +
            "map votes, events, act transition, or treasure picks.");
    }
}
