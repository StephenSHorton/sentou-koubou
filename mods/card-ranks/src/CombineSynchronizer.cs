using System.Collections.Concurrent;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;

namespace CardRanks;

/// <summary>
/// Multiplayer combine: local owner selects 3 cards and applies; peers mirror via net messages.
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

    /// <summary>UI-only deck picker; never touches the synced choice-id stream.</summary>
    private static async Task<IEnumerable<CardModel>> PickCardsFromDeck(
        Player owner,
        CardSelectorPrefs prefs,
        IReadOnlyList<CardModel>? cardsOverride = null)
    {
        List<CardModel> cards = cardsOverride?.ToList() ?? owner.Deck.Cards.ToList();
        if (cards.Count == 0)
        {
            return Enumerable.Empty<CardModel>();
        }
        NDeckCardSelectScreen screen = NDeckCardSelectScreen.Create(cards, prefs);
        NOverlayStack.Instance.Push(screen);
        try
        {
            return await screen.CardsSelected();
        }
        catch (TaskCanceledException)
        {
            // Screen was torn down (e.g. room ended) without a pick.
            return Enumerable.Empty<CardModel>();
        }
    }

    public async Task<bool> RunLocalCampfireCombine(Player owner)
    {
        Loc.EnsureCardSelectionEntries();
        var prefs = new CardSelectorPrefs(
            new MegaCrit.Sts2.Core.Localization.LocString("card_selection", "TO_COMBINE"),
            RankMath.CardsPerCombine)
        {
            Cancelable = true,
            RequireManualConfirmation = true,
        };

        // Only list cards that sit in a full 3+ same-id/same-tier bucket.
        // Otherwise the picker shows the whole deck with most rows permanently dimmed.
        List<CardModel> combinable = CombineService.GetCombinableDeckCards(owner);
        if (combinable.Count < RankMath.CardsPerCombine)
        {
            MainFile.Logger.Info(
                $"Combine picker skipped: only {combinable.Count} combinable card(s) in deck.");
            return false;
        }

        // Drive the deck-select screen directly instead of CardSelectCmd.FromDeckGeneric:
        // the Cmd reserves an id from the synced PlayerChoiceSynchronizer, which only ticks
        // on the combining client (mirrors never run this flow) and desyncs the run checksum
        // (see trading-post PR #13 — same failure, confirmed via RitsuLib divergence dump).
        // The combine itself is synced by CombineCardsMessage, so no synced choice is needed.
        IEnumerable<CardModel> selection = await PickCardsFromDeck(owner, prefs, combinable);

        List<CardModel> picked = selection.ToList();
        if (picked.Count < RankMath.CardsPerCombine)
            return false;

        if (!CombineService.CanGroup(picked))
        {
            MainFile.Logger.Info(
                "Selection rejected after picker: " +
                string.Join(" | ", picked.Select(CombineService.Describe)));
            return false;
        }

        CombineService.SplitSurvivorAndSacrifices(
            picked, out CardModel survivor, out CardModel sac1, out CardModel sac2);

        try
        {
            int sac1Rank = (int)CombineService.GetRank(sac1);
            int sac2Rank = (int)CombineService.GetRank(sac2);
            int survRank = (int)CombineService.GetRank(survivor);
            int sac1Up = sac1.CurrentUpgradeLevel;
            int sac2Up = sac2.CurrentUpgradeLevel;
            int survUp = survivor.CurrentUpgradeLevel;
            string category = survivor.Id.Category;
            string entry = survivor.Id.Entry;
            CardRankLevel resultTier = RankMath.NextRank(CombineService.GetRank(survivor));
            int maxUp = Math.Max(
                Math.Max(survivor.MaxUpgradeLevel, sac1.MaxUpgradeLevel),
                Math.Max(sac2.MaxUpgradeLevel, sac1Up + sac2Up + survUp));
            int resultUp = RankMath.SumUpgradeLevels([sac1Up, sac2Up, survUp], maxUp);

            await CombineService.ApplyLocalAsync(sac1, sac2, survivor);

            TierBonus bonus = await RankUi.AutoGrantBonusAndShowcaseAsync(
                sac1,
                sac2,
                survivor,
                resultTier,
                removeSacrificesAsync: () => CombineService.RemoveSacrificesAsync(sac1, sac2));

            _gameService.SendMessage(new CombineCardsMessage
            {
                ownerNetId = owner.NetId,
                category = category,
                entry = entry,
                sacrifice1Rank = sac1Rank,
                sacrifice1Upgrade = sac1Up,
                sacrifice2Rank = sac2Rank,
                sacrifice2Upgrade = sac2Up,
                survivorRank = survRank,
                survivorUpgrade = survUp,
                resultRank = (int)resultTier,
                resultUpgradeLevel = resultUp,
                bonusRolled = (int)bonus,
                Location = _messageBuffer.CurrentLocation,
            });

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
