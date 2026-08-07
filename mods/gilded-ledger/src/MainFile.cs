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

        // No Harmony — earlier ScrollContainer wrap on NEventLayout.AddOptions broke Neow
        // (container grew with content / stole layout space; wheel had nothing to scroll).
        // Enchant lists use in-event pagination instead.

        Logger.Info(
            "Gilded Ledger loaded — ?-room event: lose all gold for any enchantment "
            + $"(paged ×{GildedLedgerEvent.EnchantmentsPerPage}), or remove any number of cards.");
    }
}
