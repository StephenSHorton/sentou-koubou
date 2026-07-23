using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves;

namespace UncappedChapterFix;

/// <summary>
/// UncappedSpire workshop builds (≤0.3.15) ship a Harmony prefix on
/// <see cref="PlayerRngSet.LoadFromSerializable"/> compiled against the old API:
/// <c>UInt64 PlayerRngSet.get_Seed()</c>.
///
/// Current STS2 (0.107.x) exposes <c>uint Seed</c>. On first multiplayer player sync
/// the prefix JITs and throws:
/// <c>MissingMethodException: Method not found: 'UInt64 ...PlayerRngSet.get_Seed()'</c>
/// which aborts <c>StartNewMultiplayerRun</c>.
///
/// Fix: remove UncappedSpire's prefix and re-apply the same logic against <c>uint</c>.
/// Soft-depends on UncappedSpire (no-op if the type is missing).
/// </summary>
public static class PlayerRngSeedCompatPatches
{
    private const string UncappedHarmonyId = "UncappedSpire";

    private const string BrokenPrefixTypeName =
        "UncappedSpire.UncappedSpireCode.UncappedActs.PlayerRngSetPatches.Patch_LoadFromSerializable";

    public static bool TryApply(Harmony harmony)
    {
        MethodInfo? original = AccessTools.Method(
            typeof(PlayerRngSet),
            nameof(PlayerRngSet.LoadFromSerializable));
        if (original == null)
        {
            MainFile.Logger.Warn(
                "PlayerRngSet.LoadFromSerializable not found — seed compat skipped.");
            return false;
        }

        Type? brokenType = AccessTools.TypeByName(BrokenPrefixTypeName);
        if (brokenType == null)
        {
            MainFile.Logger.Info(
                "UncappedSpire PlayerRngSet seed patch type not found — seed compat skipped.");
            return false;
        }

        // Drop the broken UInt64-Seed prefix (Harmony id "UncappedSpire").
        harmony.Unpatch(original, HarmonyPatchType.Prefix, UncappedHarmonyId);

        // Same intent as upstream: force save.Seed to match the live instance so
        // vanilla LoadFromSerializable does not throw when seeds differ during
        // chapter reseed / multiplayer sync — but compiled against uint Seed.
        harmony.Patch(
            original,
            prefix: new HarmonyMethod(typeof(PlayerRngSeedCompatPatches), nameof(Prefix)));

        MainFile.Logger.Info(
            "Patched PlayerRngSet.LoadFromSerializable prefix " +
            "(replaced UncappedSpire UInt64 Seed with uint).");
        return true;
    }

    /// <summary>
    /// Mirrors UncappedSpire's Prefix body with the current Seed type.
    /// </summary>
    public static void Prefix(PlayerRngSet __instance, SerializablePlayerRngSet save)
    {
        save.Seed = __instance.Seed;
    }
}
