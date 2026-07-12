using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Blake.BlakeCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Blake";
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Logger.Info("Blake loaded — show me your moves.");

        var harmony = new Harmony(ModId);
        harmony.PatchAll();
        CookieCursorBridge.TryRegister();
    }
}
