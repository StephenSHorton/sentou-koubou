using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace BattleBall;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "BattleBall";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var harmony = new Harmony(ModId);
        harmony.PatchAll();
        Logger.Info(
            "Battle Ball loaded — combat-only ball toss (mid-screen floor, drag to throw). " +
            "Does not use native Box2D; lightweight Godot-side integration for MP sync.");
    }
}
