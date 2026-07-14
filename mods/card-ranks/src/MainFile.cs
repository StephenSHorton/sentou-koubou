using BaseLib.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace CardRanks;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "CardRanks";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static CardRanksConfig Config { get; private set; } = null!;

    public static void Initialize()
    {
        // Must construct before Register: ctor discovers static properties and Load()s disk.
        // Instance properties are ignored by BaseLib ("only static properties are supported")
        // and Register silently no-ops when HasSettings() is false — that hid the whole menu.
        Config = new CardRanksConfig();
        ModConfigRegistry.Register(ModId, Config);
        try
        {
            Loc.EnsureSettingsEntries();
        }
        catch (Exception e)
        {
            Logger.Warn($"Settings loc inject deferred: {e.Message}");
        }
        Logger.Info(
            $"Card Ranks loaded — Tier I×1.5 / II×2 / III×3, auto bonus each tier. " +
            $"AllowStrikeDefend={CardRanksConfig.AllowCombineStrikeDefend}, " +
            $"SpendAction={CardRanksConfig.SpendCampfireAction}");
        var harmony = new Harmony(ModId);
        harmony.PatchAll();
    }
}
