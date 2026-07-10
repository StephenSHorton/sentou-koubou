using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Henro.HenroCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Henro";
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Logger.Info("Henro (遍路) loaded — sentou-koubou vertical slice.");

        // Uncomment when this mod ships Godot scripts for custom scenes:
        // Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        var harmony = new Harmony(ModId);
        harmony.PatchAll();
    }
}
