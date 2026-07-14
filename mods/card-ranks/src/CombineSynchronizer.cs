using System.Collections.Concurrent;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace CardRanks;

/// <summary>
/// Multiplayer combine: local owner selects cards and applies; peers mirror via net messages.
/// Modeled on Trading Post's TradeSynchronizer / OneOffSynchronizer pattern.
/// </summary>
public sealed class CombineSynchronizer : IDisposable
{
    public static CombineSynchronizer? Instance { get; set; }

    private readonly RunLocationTargetedMessageBuffer _messageBuffer;
    private readonly INetGameService _gameService;
    private readonly IPlayerCollection _playerCollection;
    private readonly ulong _localPlayerId;
    private readonly ConcurrentDictionary<ulong, TaskCompletionSource<bool>> _campfireResults = new();

    public CombineSynchronizer(
        RunLocationTargetedMessageBuffer messageBuffer,
        INetGameService gameService,
        IPlayerCollection playerCollection,
        ulong localPlayerId)
    {
        _messageBuffer = messageBuffer;
        _gameService = gameService;
        _playerCollection = playerCollection;
        _localPlayerId = localPlayerId;
        messageBuffer.RegisterMessageHandler<CombineCardsMessage>(HandleCombineCards);
        messageBuffer.RegisterMessageHandler<CampfireCombineResultMessage>(HandleCampfireResult);
    }

    public void Dispose()
    {
        _messageBuffer.UnregisterMessageHandler<CombineCardsMessage>(HandleCombineCards);
        _messageBuffer.UnregisterMessageHandler<CampfireCombineResultMessage>(HandleCampfireResult);
    }

    public async Task<bool> RunLocalCampfireCombine(Player owner)
    {
        Loc.EnsureCardSelectionEntries();
        var prefs = new CardSelectorPrefs(new MegaCrit.Sts2.Core.Localization.LocString("card_selection", "TO_COMBINE"), 2)
        {
            Cancelable = true,
            RequireManualConfirmation = true,
        };

        // No pre-filter (RankUpCards2 style): show the full deck. Filtering candidates
        // before layout can leave the grid in a weird size/position after deck mutations.
        // Pair rules still enforced in CheckIfSelectionComplete + CanPair below.
        IEnumerable<CardModel> selection =
            await MegaCrit.Sts2.Core.Commands.CardSelectCmd.FromDeckGeneric(
                owner, prefs, filter: null, sortingOrder: null);

        List<CardModel> picked = selection.ToList();
        if (picked.Count < 2)
            return false;

        CardModel a = picked[0];
        CardModel b = picked[1];

        // Hard gate (ranks must match). Logs describe both cards if rejected.
        if (CombineService.GetRank(a) != CombineService.GetRank(b)
            || !CombineService.CanPair(a, b))
        {
            MainFile.Logger.Info(
                $"Selection rejected after picker: {CombineService.Describe(a)} | {CombineService.Describe(b)}");
            return false;
        }

        // Prefer the higher-upgrade card as survivor so we build on the stronger copy,
        // then sum upgrade levels onto it.
        CardModel sacrifice = a;
        CardModel survivor = b;
        if (a.CurrentUpgradeLevel > b.CurrentUpgradeLevel)
        {
            sacrifice = b;
            survivor = a;
        }

        try
        {
            CombineCardsMessage msg = CombineService.BuildMessage(sacrifice, survivor, owner);
            await CombineService.ApplyLocalAsync(sacrifice, survivor);
            msg.Location = _messageBuffer.CurrentLocation;
            _gameService.SendMessage(msg);

            // Let overlay teardown + rest-site layout finish before OnSelect returns.
            await Task.Yield();
            return true;
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Combine failed: {e}");
            return false;
        }
    }

    public async Task<bool> AwaitCampfireResult(ulong ownerNetId)
    {
        TaskCompletionSource<bool> tcs = _campfireResults.GetOrAdd(ownerNetId,
            _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
        bool result = await tcs.Task;
        _campfireResults.TryRemove(ownerNetId, out _);
        return result;
    }

    public void BroadcastCampfireResult(bool success)
    {
        _gameService.SendMessage(new CampfireCombineResultMessage
        {
            success = success,
            Location = _messageBuffer.CurrentLocation,
        });
    }

    private void HandleCombineCards(CombineCardsMessage message, ulong senderId)
    {
        if (senderId == _localPlayerId)
            return;
        Player? owner = _playerCollection.GetPlayer(message.ownerNetId);
        if (owner == null)
        {
            MainFile.Logger.Warn($"Remote combine: unknown owner net id {message.ownerNetId}");
            return;
        }
        TaskHelper.RunSafely(CombineService.ApplyRemoteAsync(owner, message));
    }

    private void HandleCampfireResult(CampfireCombineResultMessage message, ulong senderId)
    {
        _campfireResults.GetOrAdd(senderId,
                _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously))
            .TrySetResult(message.success);
    }
}
