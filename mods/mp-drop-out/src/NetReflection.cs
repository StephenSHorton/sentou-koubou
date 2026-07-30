using System.Collections;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;

namespace MpDropOut;

/// <summary>
/// Reflection helpers for swapping the active <see cref="INetGameService"/> mid-run
/// during host migration (copy handlers, rebind synchronizer fields, replace RunLobby).
/// </summary>
internal static class NetReflection
{
    public static object? GetMessageBus(INetGameService service)
    {
        FieldInfo? field = AccessTools.Field(service.GetType(), "_messageBus");
        return field?.GetValue(service);
    }

    /// <summary>
    /// Copy all registered message handlers from <paramref name="sourceBus"/> to
    /// <paramref name="destBus"/> so synchronizers keep receiving packets after a service swap.
    /// </summary>
    public static void CopyMessageHandlers(object sourceBus, object destBus)
    {
        FieldInfo handlersField = AccessTools.Field(typeof(NetMessageBus), "_messageHandlers")
            ?? throw new InvalidOperationException("NetMessageBus._messageHandlers missing");
        object? sourceMap = handlersField.GetValue(sourceBus);
        object? destMap = handlersField.GetValue(destBus);
        if (sourceMap is not IDictionary sourceDict || destMap is not IDictionary destDict)
            throw new InvalidOperationException("Message handler maps are not dictionaries");

        foreach (DictionaryEntry entry in sourceDict)
        {
            Type msgType = (Type)entry.Key;
            // Each value is List<CallbackPair> — clone list into dest
            if (entry.Value is not IList sourceList)
                continue;

            if (!destDict.Contains(msgType))
            {
                // Create List<CallbackPair> of same runtime type
                Type listType = sourceList.GetType();
                destDict[msgType] = Activator.CreateInstance(listType)!;
            }

            IList destList = (IList)destDict[msgType]!;
            foreach (object pair in sourceList)
                destList.Add(pair);
        }

        MainFile.Logger.Info(
            $"Copied message handlers for {sourceDict.Count} message type(s) to new net service.");
    }

    public static void SetRunManagerNetService(INetGameService service)
    {
        PropertyInfo? prop = AccessTools.Property(typeof(RunManager), nameof(RunManager.NetService));
        if (prop == null)
            throw new InvalidOperationException("RunManager.NetService property missing");
        prop.SetValue(RunManager.Instance, service);
    }

    /// <summary>
    /// Replace private <c>_netService</c> (or similar) fields that still point at the dead service.
    /// </summary>
    public static void RebindNetServiceFields(INetGameService oldService, INetGameService newService)
    {
        RunManager run = RunManager.Instance;
        object?[] holders =
        [
            run.ChecksumTracker,
            run.RunLocationTargetedBuffer,
            run.FlavorSynchronizer,
            run.ActionQueueSynchronizer,
            run.PlayerChoiceSynchronizer,
            run.MapSelectionSynchronizer,
            run.EventSynchronizer,
            run.RewardSynchronizer,
            run.RewardsSetSynchronizer,
            run.RestSiteSynchronizer,
            run.OneOffSynchronizer,
            run.TreasureRoomRelicSynchronizer,
            run.CombatStateSynchronizer,
            run.InputSynchronizer,
            run.CombatReplayWriter,
        ];

        int rebound = 0;
        foreach (object? holder in holders)
        {
            if (holder == null)
                continue;
            rebound += RebindFieldsOn(holder, oldService, newService);
        }

        MainFile.Logger.Info($"Rebound net service fields on {rebound} member(s).");
    }

    private static int RebindFieldsOn(object target, INetGameService oldService, INetGameService newService)
    {
        int n = 0;
        foreach (FieldInfo field in AccessTools.GetDeclaredFields(target.GetType()))
        {
            if (!typeof(INetGameService).IsAssignableFrom(field.FieldType)
                && field.FieldType != typeof(INetHostGameService)
                && field.FieldType != typeof(INetClientGameService)
                && field.FieldType != typeof(NetHostGameService)
                && field.FieldType != typeof(NetClientGameService))
                continue;

            object? current = field.GetValue(target);
            if (ReferenceEquals(current, oldService) || current is INetGameService)
            {
                // Prefer rebinding any INetGameService field (synchronizers only hold one).
                if (field.FieldType.IsInstanceOfType(newService)
                    || field.FieldType.IsAssignableFrom(newService.GetType()))
                {
                    field.SetValue(target, newService);
                    n++;
                }
            }
        }

        return n;
    }

    public static void ReplaceRunLobby(INetGameService newService, IEnumerable<ulong> connectedIds)
    {
        RunManager run = RunManager.Instance;
        RunState state = run.State
            ?? throw new InvalidOperationException("No run state during lobby replace");

        RunLobby? old = run.RunLobby;
        if (old != null)
        {
            try
            {
                old.Dispose();
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"Old RunLobby dispose: {e.Message}");
            }
        }

        // RunLobby ctor: (GameMode, INetGameService, IRunLobbyListener, IPlayerCollection, IEnumerable<ulong>)
        var lobby = new RunLobby(
            state.GameMode,
            newService,
            run,
            state,
            connectedIds);

        PropertyInfo? lobbyProp = AccessTools.Property(typeof(RunManager), nameof(RunManager.RunLobby));
        lobbyProp?.SetValue(run, lobby);

        // Re-subscribe disconnect → drop-out (vanilla RemotePlayerDisconnected is private;
        // RunLobby event RemotePlayerDisconnected is public)
        lobby.RemotePlayerDisconnected += playerId =>
        {
            // Invoke private RunManager.RemotePlayerDisconnected via AccessTools
            MethodInfo? method = AccessTools.Method(typeof(RunManager), "RemotePlayerDisconnected");
            method?.Invoke(run, [playerId]);
        };

        // CombatStateSynchronizer holds RunLobby — rebuild or set field
        try
        {
            run.CombatStateSynchronizer?.Dispose();
        }
        catch
        {
            // ignore
        }

        var combatSync = new MegaCrit.Sts2.Core.Multiplayer.CombatStateSynchronizer(
            newService, lobby, state);
        PropertyInfo? cssProp = AccessTools.Property(
            typeof(RunManager), nameof(RunManager.CombatStateSynchronizer));
        cssProp?.SetValue(run, combatSync);

        MainFile.Logger.Info(
            $"Replaced RunLobby as {newService.Type}; connected seed=[{string.Join(",", connectedIds)}]");
    }
}
