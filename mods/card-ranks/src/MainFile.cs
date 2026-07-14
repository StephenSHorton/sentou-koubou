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
        Config = new CardRanksConfig();
        ModConfigRegistry.Register(ModId, Config);
        Logger.Info("Card Ranks loaded — manual campfire combine (Rank 2 ×1.5 / Rank 3 ×3).");
        var harmony = new Harmony(ModId);
        harmony.PatchAll();
    }
}
