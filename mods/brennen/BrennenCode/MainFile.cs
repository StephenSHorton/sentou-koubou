using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Brennen.BrennenCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Brennen";
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Logger.Info("Brennen loaded — family meme pack, character 1. Queue up.");

        var harmony = new Harmony(ModId);
        harmony.PatchAll();
    }
}
