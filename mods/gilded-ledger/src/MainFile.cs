using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace GildedLedger;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "GildedLedger";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        // ModelDb discovers CustomEventModel subtypes; constructing once ensures
        // BaseLib registers the event into the shared-act pool even if discovery order differs.
        _ = new GildedLedgerEvent();

        var harmony = new Harmony(ModId);
        harmony.PatchAll();

        Logger.Info(
            "Gilded Ledger loaded — ?-room event: lose all gold for any enchantment, "
            + "or remove any number of cards (scrollable enchant list).");
    }
}
