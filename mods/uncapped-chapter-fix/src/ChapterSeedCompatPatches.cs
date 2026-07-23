using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

namespace UncappedChapterFix;

/// <summary>
/// UncappedSpire ≤0.3.15 <c>ChapterChangeSynchronizer.DoSeedChange</c> is compiled against
/// <c>UInt64 StringHelper.GetDeterministicHashCode(string)</c>. Current STS2 returns <c>int</c>.
///
/// On "Through the Mysterious Door" the shared vote succeeds, then the peer receiving
/// <c>ChapterChangeMessage</c> (and host's local DoSeedChange) throws
/// <c>MissingMethodException</c> and the chapter transition hangs with no act/map.
///
/// Fix: Harmony-prefix that replaces DoSeedChange with the same logic using the current
/// <c>int</c> hash API (and <c>Rng(uint)</c>). Soft-depends on UncappedSpire.
/// </summary>
public static class ChapterSeedCompatPatches
{
    private const string SynchronizerTypeName =
        "UncappedSpire.UncappedSpireCode.UncappedActs.ChapterChangeSynchronizer";

    public static bool TryApply(Harmony harmony)
    {
        Type? syncType = AccessTools.TypeByName(SynchronizerTypeName);
        if (syncType == null)
        {
            MainFile.Logger.Info(
                "UncappedSpire ChapterChangeSynchronizer not found — chapter seed compat skipped.");
            return false;
        }

        MethodInfo? doSeedChange = AccessTools.Method(syncType, "DoSeedChange", [typeof(string)]);
        if (doSeedChange == null)
        {
            MainFile.Logger.Warn(
                "ChapterChangeSynchronizer.DoSeedChange not found — API changed?");
            return false;
        }

        harmony.Patch(
            doSeedChange,
            prefix: new HarmonyMethod(typeof(ChapterSeedCompatPatches), nameof(DoSeedChangePrefix)));

        MainFile.Logger.Info(
            "Patched ChapterChangeSynchronizer.DoSeedChange " +
            "(replaced UInt64 GetDeterministicHashCode with int).");
        return true;
    }

    /// <summary>
    /// Replaces UncappedSpire DoSeedChange. Returns false to skip the broken original body.
    /// </summary>
    public static bool DoSeedChangePrefix(object __instance, string seed, ref bool __result)
    {
        try
        {
            FieldInfo? runStateField = AccessTools.Field(__instance.GetType(), "_runState");
            FieldInfo? localIdField = AccessTools.Field(__instance.GetType(), "_localPlayerId");
            FieldInfo? gameServiceField = AccessTools.Field(__instance.GetType(), "_gameService");

            if (runStateField?.GetValue(__instance) is not RunState runState)
            {
                MainFile.Logger.Error("Chapter seed compat: missing _runState.");
                __result = false;
                return false;
            }

            ulong localPlayerId = localIdField != null
                ? (ulong)localIdField.GetValue(__instance)!
                : 0UL;
            Player? localPlayer = runState.GetPlayer(localPlayerId);
            if (localPlayer == null)
            {
                MainFile.Logger.Error($"Chapter seed compat: no player {localPlayerId}.");
                __result = false;
                return false;
            }

            // Replace run RNG (same as upstream via reflection setter).
            MethodInfo? setRng = AccessTools.PropertySetter(typeof(RunState), nameof(RunState.Rng));
            setRng?.Invoke(localPlayer.RunState, [new RunRngSet(seed)]);

            foreach (Player player in localPlayer.RunState.Players)
                player.InitializeSeed(seed);

            // Current game: GetDeterministicHashCode → int; Rng ctor takes uint.
            uint actSeed = (uint)StringHelper.GetDeterministicHashCode(seed);
            var rng = new Rng(actSeed);

            object? netService = gameServiceField?.GetValue(__instance);
            bool isMultiplayer = ResolveIsMultiplayer(netService);

            List<ActModel> mutableActs = ActModel
                .GetRandomList(rng, UnlockState.all, isMultiplayer)
                .Select(a => a.ToMutable())
                .ToList();
            foreach (ActModel act in mutableActs)
                act.AssertMutable();

            MethodInfo? setActs = AccessTools.PropertySetter(typeof(RunState), nameof(RunState.Acts));
            setActs?.Invoke(localPlayer.RunState, [mutableActs]);

            TryAscensionIncrease(localPlayer);
            RunManager.Instance.GenerateRooms();
            TryRefreshTopBar(localPlayer);

            MainFile.Logger.Info(
                $"Chapter seed change applied (seed={seed}, acts={mutableActs.Count}, mp={isMultiplayer}).");
            __result = true;
            return false; // skip broken UInt64 body
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Chapter seed compat failed: {e}");
            __result = false;
            return false;
        }
    }

    private static bool ResolveIsMultiplayer(object? netService)
    {
        if (netService == null)
            return true; // safe default for chapter reseed in MP

        try
        {
            object? typeObj = AccessTools.Property(netService.GetType(), "Type")
                ?.GetValue(netService);
            if (typeObj is NetGameType netType)
                return netType.IsMultiplayer();
            return true;
        }
        catch
        {
            return true;
        }
    }

    private static void TryAscensionIncrease(Player localPlayer)
    {
        try
        {
            Type? ctx = AccessTools.TypeByName(
                "UncappedSpire.UncappedSpireCode.Config.ContextManager");
            PropertyInfo? enabled = AccessTools.Property(ctx, "AscensionIncreaseEnabled");
            if (enabled == null || !(bool)enabled.GetValue(null)!)
                return;

            Type? asc = AccessTools.TypeByName(
                "UncappedSpire.UncappedSpireCode.UncappedActs.AscensionIncrease")
                ?? AccessTools.TypeByName(
                    "UncappedSpire.UncappedSpireCode.Config.AscensionIncrease");
            MethodInfo? inc = AccessTools.Method(asc, "IncrementAscension", [typeof(Player)]);
            inc?.Invoke(null, [localPlayer]);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"AscensionIncrease skip: {e.Message}");
        }
    }

    private static void TryRefreshTopBar(Player localPlayer)
    {
        try
        {
            Type? ui = AccessTools.TypeByName(
                "UncappedSpire.UncappedSpireCode.UncappedActs.ChapterChangeUiRefresh");
            MethodInfo? refresh = AccessTools.Method(ui, "RefreshTopBar", [typeof(Player)]);
            refresh?.Invoke(null, [localPlayer]);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"ChapterChangeUiRefresh skip: {e.Message}");
        }
    }
}
