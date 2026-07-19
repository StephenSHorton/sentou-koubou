using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace BattleDraw;

[ModInitializer(nameof(Initialize))]
public class MainFile
{
    public const string ModId = "BattleDraw";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        Logger.Info(
            "Battle Draw loaded — middle-mouse (or Alt+LMB) to sketch on combat; " +
            "never steals card clicks; clears when combat ends.");
        var harmony = new Harmony(ModId);
        harmony.PatchAll();
    }
}
