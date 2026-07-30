using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Transport.Steam;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace MpDropOut;

/// <summary>
/// When the host disconnects, elect a successor (lowest remaining NetId), promote them to
/// Steam host, and reconnect other clients to that lobby — keeping the run alive.
/// </summary>
public static class HostMigration
{
    private static int _migrating; // 0/1 interlocked
    private static ulong _lastKnownHostId;

    /// <summary>Track host id while connected so we can elect after the client socket dies.</summary>
    public static void NoteHostId(ulong hostId)
    {
        if (hostId != 0)
            _lastKnownHostId = hostId;
    }

    public static void NoteHostFromService(INetGameService? service)
    {
        try
        {
            if (service is NetClientGameService client && client.IsConnected)
                NoteHostId(client.HostNetId);
            else if (service is NetHostGameService host && host.IsConnected)
                NoteHostId(host.NetId);
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// Returns true if migration was started (caller should skip return-to-menu).
    /// </summary>
    public static bool TryBeginOnLocalDisconnect(NetErrorInfo info)
    {
        try
        {
            if (Interlocked.CompareExchange(ref _migrating, 1, 0) != 0)
            {
                MainFile.Logger.Info("Host migration already in progress.");
                return true;
            }

            RunManager run = RunManager.Instance;
            if (run.State == null || run.IsAbandoned || run.State.IsGameOver)
            {
                Interlocked.Exchange(ref _migrating, 0);
                return false;
            }

            NetError reason = info.GetReason();
            // Don't migrate on intentional abandon / game over.
            if (reason is NetError.HostAbandoned or NetError.QuitGameOver)
            {
                Interlocked.Exchange(ref _migrating, 0);
                return false;
            }

            // Only clients lose the host; if we were host, remaining peers migrate among themselves.
            if (run.NetService.Type == NetGameType.Host)
            {
                Interlocked.Exchange(ref _migrating, 0);
                return false;
            }

            ulong deadHost = _lastKnownHostId;
            try
            {
                if (run.NetService is NetClientGameService c)
                    deadHost = c.HostNetId;
            }
            catch
            {
                // HostNetId may throw if already torn down
            }

            if (deadHost == 0)
            {
                MainFile.Logger.Warn("Host migration aborted: unknown dead host id.");
                Interlocked.Exchange(ref _migrating, 0);
                return false;
            }

            List<Player> remaining = run.State.Players
                .Where(p => p.NetId != deadHost)
                .OrderBy(p => p.NetId)
                .ToList();

            if (remaining.Count == 0)
            {
                MainFile.Logger.Warn("Host migration aborted: no remaining players.");
                Interlocked.Exchange(ref _migrating, 0);
                return false;
            }

            ulong successorId = remaining[0].NetId;
            ulong me = MegaCrit.Sts2.Core.Context.LocalContext.NetId
                       ?? run.NetService.NetId;

            MainFile.Logger.Info(
                $"Host {deadHost} lost (reason={reason}). Electing successor {successorId}. " +
                $"Local={me}. Remaining=[{string.Join(",", remaining.Select(p => p.NetId))}]");

            TaskHelper.RunSafely(MigrateAsync(deadHost, successorId, me, remaining));
            return true;
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"TryBeginOnLocalDisconnect failed: {e}");
            Interlocked.Exchange(ref _migrating, 0);
            return false;
        }
    }

    private static async Task MigrateAsync(
        ulong deadHost,
        ulong successorId,
        ulong localId,
        List<Player> remaining)
    {
        try
        {
            bool isSuccessor = localId == successorId;
            if (isSuccessor)
                await PromoteToHostAsync(deadHost, remaining);
            else
                await ReconnectAsClientAsync(successorId, remaining);

            // Drop the dead host from shared waits.
            DropOutService.OnRemoteDisconnected(deadHost);

            MainFile.Logger.Info(
                $"Host migration complete. Local is now {RunManager.Instance.NetService.Type}.");
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Host migration failed: {e}");
            // Leave the party in a broken net state rather than force-kill mid-migration;
            // player can quit to menu. Returning to menu from here races the live room.
        }
        finally
        {
            Interlocked.Exchange(ref _migrating, 0);
        }
    }

    private static async Task PromoteToHostAsync(ulong deadHost, List<Player> remaining)
    {
        RunManager run = RunManager.Instance;
        INetGameService oldService = run.NetService;
        object? oldBus = NetReflection.GetMessageBus(oldService);

        MainFile.Logger.Info("Promoting local player to multiplayer host…");

        var hostService = new NetHostGameService();
        int maxPlayers = Math.Max(remaining.Count + 1, 4);
        NetErrorInfo? startErr = await hostService.StartSteamHost(maxPlayers);
        if (startErr.HasValue)
            throw new InvalidOperationException($"StartSteamHost failed: {startErr.Value.GetReason()}");

        object? newBus = NetReflection.GetMessageBus(hostService)
            ?? throw new InvalidOperationException("New host has no message bus");
        if (oldBus != null)
            NetReflection.CopyMessageHandlers(oldBus, newBus);

        NetReflection.SetRunManagerNetService(hostService);
        NetReflection.RebindNetServiceFields(oldService, hostService);

        // Connected set = remaining players still in the run (including us). Dead host excluded.
        IEnumerable<ulong> seed = remaining.Select(p => p.NetId);
        NetReflection.ReplaceRunLobby(hostService, seed);

        try
        {
            PlatformUtil.SetRichPresence(
                "IN_RUN",
                hostService.GetRawLobbyIdentifier(),
                remaining.Count);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"SetRichPresence: {e.Message}");
        }

        NoteHostId(hostService.NetId);

        // Quietly disconnect old client transport if still half-open.
        try
        {
            if (oldService.IsConnected)
                oldService.Disconnect(NetError.Quit, now: true);
        }
        catch
        {
            // ignore
        }

        MainFile.Logger.Info(
            $"Now hosting Steam lobby {hostService.GetRawLobbyIdentifier()}. " +
            "Waiting for peers to rejoin…");
    }

    private static async Task ReconnectAsClientAsync(ulong successorId, List<Player> remaining)
    {
        RunManager run = RunManager.Instance;
        INetGameService oldService = run.NetService;
        object? oldBus = NetReflection.GetMessageBus(oldService);

        MainFile.Logger.Info($"Reconnecting to new host {successorId}…");

        // Give successor time to create the Steam lobby.
        const int maxAttempts = 15;
        NetClientGameService? clientService = null;
        Exception? last = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                clientService = new NetClientGameService();
                var steamClient = new SteamClient(clientService);
                clientService.Initialize(steamClient, PlatformType.Steam);

                NetErrorInfo? err =
                    await steamClient.ConnectToLobbyOwnedByFriend(successorId);
                if (err.HasValue)
                    throw new InvalidOperationException(
                        $"Connect attempt {attempt} failed: {err.Value.GetReason()}");

                last = null;
                break;
            }
            catch (Exception e)
            {
                last = e;
                MainFile.Logger.Warn(
                    $"Join successor attempt {attempt}/{maxAttempts}: {e.Message}");
                clientService = null;
                await Task.Delay(1000);
            }
        }

        if (clientService == null || last != null && !clientService.IsConnected)
            throw last ?? new InvalidOperationException("Failed to connect to successor host");

        object? newBus = NetReflection.GetMessageBus(clientService)
            ?? throw new InvalidOperationException("New client has no message bus");
        if (oldBus != null)
            NetReflection.CopyMessageHandlers(oldBus, newBus);

        NetReflection.SetRunManagerNetService(clientService);
        NetReflection.RebindNetServiceFields(oldService, clientService);

        IEnumerable<ulong> seed = remaining.Select(p => p.NetId);
        NetReflection.ReplaceRunLobby(clientService, seed);

        // Complete rejoin handshake so host marks us ready for broadcast.
        await CompleteRejoinHandshakeAsync(clientService);

        NoteHostId(successorId);

        try
        {
            if (oldService.IsConnected)
                oldService.Disconnect(NetError.Quit, now: true);
        }
        catch
        {
            // ignore
        }

        MainFile.Logger.Info($"Reconnected to new host {successorId}.");
    }

    private static async Task CompleteRejoinHandshakeAsync(NetClientGameService client)
    {
        var tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        void OnRejoin(ClientRejoinResponseMessage msg, ulong sender)
        {
            MainFile.Logger.Info(
                $"Received rejoin snapshot from host (combat={(msg.combatState != null)}).");
            tcs.TrySetResult(true);
        }

        void OnInitial(InitialGameInfoMessage msg, ulong sender)
        {
            MainFile.Logger.Info(
                $"InitialGameInfo after migration: state={msg.sessionState} fail={msg.connectionFailureReason}");
            if (msg.connectionFailureReason.HasValue)
            {
                tcs.TrySetException(new InvalidOperationException(
                    $"Host rejected reconnect: {msg.connectionFailureReason}"));
                return;
            }

            // Host accepted connection; request rejoin payload / broadcast readiness.
            client.SendMessage(default(ClientRejoinRequestMessage));
        }

        client.RegisterMessageHandler<InitialGameInfoMessage>(OnInitial);
        client.RegisterMessageHandler<ClientRejoinResponseMessage>(OnRejoin);
        try
        {
            // Host may already have sent InitialGameInfo before we registered — also probe.
            client.SendMessage(default(ClientRejoinRequestMessage));

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await using (cts.Token.Register(() =>
                             tcs.TrySetException(new TimeoutException("Rejoin handshake timed out"))))
            {
                await tcs.Task;
            }
        }
        finally
        {
            client.UnregisterMessageHandler<InitialGameInfoMessage>(OnInitial);
            client.UnregisterMessageHandler<ClientRejoinResponseMessage>(OnRejoin);
        }
    }
}
