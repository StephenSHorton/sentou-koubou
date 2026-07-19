using BaseLib.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace BattleDraw;

[ModInitializer(nameof(Initialize))]
public class MainFile
{
    public const string ModId = "BattleDraw";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static BrushConfig Config { get; private set; } = null!;

    public static void Initialize()
    {
        // Construct before Register so static props are discovered + loaded from disk.
        Config = new BrushConfig();
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
            "Battle Draw loaded — middle-mouse / Alt+LMB combat doodle; " +
            $"brush size={BrushConfig.ClampedSize:0.#} color={BrushConfig.ColorPreset}; " +
            "map pen uses the same settings for your strokes. " +
            "Hotkeys: [ / ] size, ; / ' color cycle.");
        var harmony = new Harmony(ModId);
        harmony.PatchAll();
    }
}
