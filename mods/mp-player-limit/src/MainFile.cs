using BaseLib.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace MpPlayerLimit;

/// <summary>
/// Clean multiplayer capacity raise for STS2.
/// Vanilla hardcodes <c>4</c> when hosting (Steam/ENet) and when building
/// <see cref="MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.StartRunLobby"/>.
/// We rewrite those 4s — no workshop RMP, no per-frame lobby hacks.
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "MpPlayerLimit";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static MpLimitConfig Config { get; private set; } = null!;

    public static void Initialize()
    {
        Config = new MpLimitConfig();
        ModConfigRegistry.Register(ModId, Config);

        var harmony = new Harmony(ModId);
        harmony.PatchAll();

        Logger.Info(
            $"Multiplayer Player Limit loaded — host/lobby capacity {MpLimitConfig.ClampedMax} " +
            $"(vanilla {MpLimitConfig.VanillaMax}). All clients should use the same max.");
    }
}
