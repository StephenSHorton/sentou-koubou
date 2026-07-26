using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace TradingPost;

[ModInitializer(nameof(Initialize))]
public class MainFile
{
    public const string ModId = "TradingPost";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        Logger.Info("Trading Post loaded — co-op gold gifts, campfire cards, shop sell (potions/relics).");
        var harmony = new Harmony(ModId);
        harmony.PatchAll();
    }
}
