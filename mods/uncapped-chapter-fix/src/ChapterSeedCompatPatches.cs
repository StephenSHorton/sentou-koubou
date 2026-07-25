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
/// On "Through the Mysterious Door" the shared vote succeeds, then DoSeedChange throws
/// <c>MissingMethodException</c> and the chapter transition hangs.
///
/// Important: Harmony cannot patch <c>DoSeedChange</c> itself — reading its IL fails with the
/// same MissingMethodException when resolving the dead UInt64 call token. We instead prefix
/// the two callers whose bodies are clean:
///   • <c>DoLocalSeedChange</c> (local vote path — also broadcasts ChapterChangeMessage)
///   • <c>HandleChapterChangeMessage</c> (remote peer path)
/// </summary>
public static class ChapterSeedCompatPatches
{
    private const string SynchronizerTypeName =
        "UncappedSpire.UncappedSpireCode.UncappedActs.ChapterChangeSynchronizer";

    private const string MessageTypeName =
        "UncappedSpire.UncappedSpireCode.UncappedActs.ChapterChangeMessage";

    public static bool TryApply(Harmony harmony)
    {
        try
        {
            Type? syncType = AccessTools.TypeByName(SynchronizerTypeName);
            if (syncType == null)
            {
                MainFile.Logger.Info(
                    "UncappedSpire ChapterChangeSynchronizer not found — chapter seed compat skipped.");
                return false;
            }

            int patched = 0;

            MethodInfo? doLocal = AccessTools.Method(syncType, "DoLocalSeedChange", [typeof(string)]);
            if (doLocal != null)
            {
                // Do not patch DoSeedChange — Harmony MethodBodyReader dies on the UInt64 token.
                harmony.Patch(
                    doLocal,
                    prefix: new HarmonyMethod(
                        typeof(ChapterSeedCompatPatches), nameof(DoLocalSeedChangePrefix)));
                patched++;
                MainFile.Logger.Info(
                    "Patched ChapterChangeSynchronizer.DoLocalSeedChange " +
                    "(bypass broken DoSeedChange UInt64 hash).");
            }
            else
            {
                MainFile.Logger.Warn("DoLocalSeedChange not found — API changed?");
            }

            MethodInfo? handle = AccessTools.Method(syncType, "HandleChapterChangeMessage");
            if (handle != null)
            {
                harmony.Patch(
                    handle,
                    prefix: new HarmonyMethod(
                        typeof(ChapterSeedCompatPatches), nameof(HandleChapterChangeMessagePrefix)));
                patched++;
                MainFile.Logger.Info(
                    "Patched ChapterChangeSynchronizer.HandleChapterChangeMessage " +
                    "(remote chapter seed reseed).");
            }
            else
            {
                MainFile.Logger.Warn("HandleChapterChangeMessage not found — API changed?");
            }

            return patched > 0;
        }
        catch (Exception e)
        {
            // Never let a failed seed patch abort the rest of UncappedChapterFix init.
            MainFile.Logger.Error($"Chapter seed compat TryApply failed: {e}");
            return false;
        }
    }

    /// <summary>
    /// Local vote path: broadcast ChapterChangeMessage, apply int-safe seed change, skip original.
    /// </summary>
    public static bool DoLocalSeedChangePrefix(object __instance, string seed)
    {
        try
        {
            BroadcastChapterChange(__instance, seed);
            if (!ApplySeedChange(__instance, seed))
            {
                MainFile.Logger.Error("DoLocalSeedChange compat: ApplySeedChange returned false.");
            }
            return false; // skip original (which would call broken DoSeedChange)
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"DoLocalSeedChange compat failed: {e}");
            // Still skip original — calling it only throws MissingMethodException.
            return false;
        }
    }

    /// <summary>
    /// Remote peer path: apply seed from message. Skip original DoSeedChange call.
    /// </summary>
    public static bool HandleChapterChangeMessagePrefix(object __instance, object message, ulong senderId)
    {
        try
        {
            // Upstream throws if the local player somehow handles their own message.
            if (IsMessageFromLocalPlayer(__instance, senderId))
            {
                MainFile.Logger.Warn(
                    "HandleChapterChangeMessage from local player — skipping seed apply.");
                return false;
            }

            FieldInfo? seedField = AccessTools.Field(message.GetType(), "seed");
            string? seed = seedField?.GetValue(message) as string;
            if (string.IsNullOrEmpty(seed))
            {
                MainFile.Logger.Error("HandleChapterChangeMessage compat: missing seed.");
                return false;
            }

            if (!ApplySeedChange(__instance, seed))
            {
                MainFile.Logger.Error("HandleChapterChangeMessage compat: ApplySeedChange failed.");
            }
            return false;
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"HandleChapterChangeMessage compat failed: {e}");
            return false;
        }
    }

    private static bool IsMessageFromLocalPlayer(object sync, ulong senderId)
    {
        try
        {
            FieldInfo? runStateField = AccessTools.Field(sync.GetType(), "_runState");
            FieldInfo? localIdField = AccessTools.Field(sync.GetType(), "_localPlayerId");
            if (runStateField?.GetValue(sync) is not RunState runState || localIdField == null)
                return false;
            ulong localId = (ulong)localIdField.GetValue(sync)!;
            return senderId == localId || runState.GetPlayer(senderId)?.NetId == localId;
        }
        catch
        {
            return false;
        }
    }

    private static void BroadcastChapterChange(object sync, string seed)
    {
        try
        {
            Type? msgType = AccessTools.TypeByName(MessageTypeName);
            FieldInfo? bufferField = AccessTools.Field(sync.GetType(), "_messageBuffer");
            FieldInfo? gameServiceField = AccessTools.Field(sync.GetType(), "_gameService");
            if (msgType == null || bufferField == null || gameServiceField == null)
            {
                MainFile.Logger.Warn("BroadcastChapterChange: missing types/fields.");
                return;
            }

            object msg = Activator.CreateInstance(msgType)!;
            AccessTools.Field(msgType, "seed")?.SetValue(msg, seed);

            object? buffer = bufferField.GetValue(sync);
            object? location = AccessTools.Property(buffer!.GetType(), "CurrentLocation")
                ?.GetValue(buffer);
            if (location != null)
                AccessTools.Field(msgType, "location")?.SetValue(msg, location);

            object? gameService = gameServiceField.GetValue(sync);
            if (gameService == null)
            {
                MainFile.Logger.Warn("BroadcastChapterChange: no game service.");
                return;
            }

            // INetGameService.SendMessage<T>(T) or non-generic SendMessage(INetMessage)
            MethodInfo? send = gameService.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m =>
                    m.Name == "SendMessage"
                    && m.IsGenericMethodDefinition
                    && m.GetParameters().Length == 1);

            if (send != null)
            {
                send.MakeGenericMethod(msgType).Invoke(gameService, [msg]);
                MainFile.Logger.Info("Broadcast ChapterChangeMessage (compat).");
                return;
            }

            MethodInfo? sendPlain = gameService.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m =>
                    m.Name == "SendMessage"
                    && !m.IsGenericMethod
                    && m.GetParameters().Length == 1);
            sendPlain?.Invoke(gameService, [msg]);
            MainFile.Logger.Info("Broadcast ChapterChangeMessage via non-generic SendMessage.");
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"BroadcastChapterChange failed: {e}");
        }
    }

    /// <summary>
    /// int-safe reimplementation of UncappedSpire DoSeedChange.
    /// </summary>
    private static bool ApplySeedChange(object sync, string seed)
    {
        try
        {
            FieldInfo? runStateField = AccessTools.Field(sync.GetType(), "_runState");
            FieldInfo? localIdField = AccessTools.Field(sync.GetType(), "_localPlayerId");
            FieldInfo? gameServiceField = AccessTools.Field(sync.GetType(), "_gameService");

            if (runStateField?.GetValue(sync) is not RunState runState)
            {
                MainFile.Logger.Error("Chapter seed compat: missing _runState.");
                return false;
            }

            ulong localPlayerId = localIdField != null
                ? (ulong)localIdField.GetValue(sync)!
                : 0UL;
            Player? localPlayer = runState.GetPlayer(localPlayerId);
            if (localPlayer == null)
            {
                MainFile.Logger.Error($"Chapter seed compat: no player {localPlayerId}.");
                return false;
            }

            MethodInfo? setRng = AccessTools.PropertySetter(typeof(RunState), nameof(RunState.Rng));
            setRng?.Invoke(localPlayer.RunState, [new RunRngSet(seed)]);

            foreach (Player player in localPlayer.RunState.Players)
                player.InitializeSeed(seed);

            // Current game: GetDeterministicHashCode → int; Rng ctor takes uint.
            uint actSeed = (uint)StringHelper.GetDeterministicHashCode(seed);
            var rng = new Rng(actSeed);

            object? netService = gameServiceField?.GetValue(sync);
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
            return true;
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Chapter seed ApplySeedChange failed: {e}");
            return false;
        }
    }

    private static bool ResolveIsMultiplayer(object? netService)
    {
        if (netService == null)
            return true;

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
