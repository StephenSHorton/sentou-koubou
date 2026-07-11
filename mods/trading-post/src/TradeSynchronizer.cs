using System.Collections.Concurrent;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace TradingPost;

/// <summary>
/// Synchronizes trades between co-op players, modeled on the game's OneOffSynchronizer.
/// Gold gifts are unlimited and available at shops; card gifts and relic requests happen
/// at campfires through <see cref="TradeRestSiteOption" /> and consume that action.
/// The initiating client applies state changes locally and broadcasts messages; every
/// other client mirrors the same change for the involved players.
/// </summary>
public class TradeSynchronizer : IDisposable
{
    public static TradeSynchronizer? Instance { get; set; }

    private readonly RunLocationTargetedMessageBuffer _messageBuffer;

    private readonly INetGameService _gameService;

    private readonly IPlayerCollection _playerCollection;

    private readonly ulong _localPlayerId;

    /// <summary>Mirrored campfire trades: outcome per trading player, keyed by net id.</summary>
    private readonly ConcurrentDictionary<ulong, TaskCompletionSource<bool>> _campfireResults = new();

    private Player LocalPlayer => _playerCollection.GetPlayer(_localPlayerId);

    public TradeSynchronizer(RunLocationTargetedMessageBuffer messageBuffer, INetGameService gameService,
        IPlayerCollection playerCollection, ulong localPlayerId)
    {
        _messageBuffer = messageBuffer;
        _gameService = gameService;
        _playerCollection = playerCollection;
        _localPlayerId = localPlayerId;
        messageBuffer.RegisterMessageHandler<GiftGoldMessage>(HandleGiftGold);
        messageBuffer.RegisterMessageHandler<GiftCardMessage>(HandleGiftCard);
        messageBuffer.RegisterMessageHandler<CampfireTradeResultMessage>(HandleCampfireResult);
    }

    public void Dispose()
    {
        _messageBuffer.UnregisterMessageHandler<GiftGoldMessage>(HandleGiftGold);
        _messageBuffer.UnregisterMessageHandler<GiftCardMessage>(HandleGiftCard);
        _messageBuffer.UnregisterMessageHandler<CampfireTradeResultMessage>(HandleCampfireResult);
    }

    public IReadOnlyList<Player> OtherPlayers =>
        _playerCollection.Players.Where(p => p.NetId != _localPlayerId).ToList();

    public static string NameOf(Player player)
    {
        return PlatformUtil.GetPlayerNameRaw(RunManager.Instance.NetService.Platform, player.NetId);
    }

    // ---------------------------------------------------------------- campfire orchestration

    /// <summary>Local player picked the campfire Trade option: run the menu-driven flow.</summary>
    public async Task<bool> RunLocalCampfireTrade()
    {
        var outcome = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        TradeUi.OpenCampfireMenu(outcome);
        bool traded = await outcome.Task;
        _gameService.SendMessage(new CampfireTradeResultMessage
        {
            success = traded,
            Location = _messageBuffer.CurrentLocation
        });
        return traded;
    }

    /// <summary>Remote mirror of a campfire trade: resolves when the trader broadcasts the outcome.</summary>
    public async Task<bool> AwaitCampfireResult(ulong traderNetId)
    {
        TaskCompletionSource<bool> tcs = _campfireResults.GetOrAdd(traderNetId,
            _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
        bool result = await tcs.Task;
        _campfireResults.TryRemove(traderNetId, out _);
        return result;
    }

    private void HandleCampfireResult(CampfireTradeResultMessage message, ulong senderId)
    {
        _campfireResults.GetOrAdd(senderId,
                _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously))
            .TrySetResult(message.success);
    }

    // ---------------------------------------------------------------- gold

    /// <summary>Local player gifts gold. Free, unlimited, available whenever the shop button shows.</summary>
    public async Task GiftGoldLocal(Player target, int amount)
    {
        amount = Math.Clamp(amount, 0, LocalPlayer.Gold);
        if (amount <= 0)
        {
            return;
        }
        try
        {
            await ApplyGoldGift(LocalPlayer, target, amount);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Gold gift failed: {e}");
            TradeUi.Notify("The trade fizzled — nothing was exchanged.");
            return;
        }
        _gameService.SendMessage(new GiftGoldMessage
        {
            targetNetId = target.NetId,
            amount = amount,
            Location = _messageBuffer.CurrentLocation
        });
    }

    private void HandleGiftGold(GiftGoldMessage message, ulong senderId)
    {
        Player giver = _playerCollection.GetPlayer(senderId);
        Player receiver = _playerCollection.GetPlayer(message.targetNetId);
        TaskHelper.RunSafely(ApplyGoldGift(giver, receiver, message.amount));
        if (receiver == LocalPlayer)
        {
            TradeUi.Notify($"{NameOf(giver)} sent you {message.amount} gold!");
        }
    }

    private static async Task ApplyGoldGift(Player giver, Player receiver, int amount)
    {
        await PlayerCmd.LoseGold(amount, giver, GoldLossType.Spent);
        await PlayerCmd.GainGold(amount, receiver);
    }

    // ---------------------------------------------------------------- cards (campfire)

    /// <summary>
    /// Local player picks a card from their deck and gifts it.
    /// Returns false if they backed out of the card picker or the transfer failed.
    /// </summary>
    public async Task<bool> GiftCardLocal(Player target)
    {
        var prefs = new CardSelectorPrefs(Loc.Get("GIVE_CARD"), 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
        CardModel? card = (await CardSelectCmd.FromDeckGeneric(LocalPlayer, prefs)).FirstOrDefault();
        if (card == null)
        {
            return false;
        }
        int upgradeLevel = card.CurrentUpgradeLevel;
        try
        {
            await ApplyCardGift(LocalPlayer, target, card.Id, upgradeLevel);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Card gift failed, trade refunded: {e}");
            TradeUi.Notify("The trade fizzled — nothing was exchanged.");
            return false;
        }
        _gameService.SendMessage(new GiftCardMessage
        {
            targetNetId = target.NetId,
            category = card.Id.Category,
            entry = card.Id.Entry,
            upgradeLevel = upgradeLevel,
            Location = _messageBuffer.CurrentLocation
        });
        return true;
    }

    private void HandleGiftCard(GiftCardMessage message, ulong senderId)
    {
        Player giver = _playerCollection.GetPlayer(senderId);
        Player receiver = _playerCollection.GetPlayer(message.targetNetId);
        var id = new ModelId(message.category, message.entry);
        TaskHelper.RunSafely(ApplyCardGift(giver, receiver, id, message.upgradeLevel));
        if (receiver == LocalPlayer)
        {
            TradeUi.Notify($"{NameOf(giver)} gave you a card: {ModelDb.GetByIdOrNull<CardModel>(id)?.Title ?? message.entry}!");
        }
    }

    private static async Task ApplyCardGift(Player giver, Player receiver, ModelId cardId, int upgradeLevel)
    {
        CardModel? original = giver.Deck.Cards
            .FirstOrDefault(c => c.Id == cardId && c.CurrentUpgradeLevel == upgradeLevel)
            ?? giver.Deck.Cards.FirstOrDefault(c => c.Id == cardId);
        if (original != null)
        {
            await CardPileCmd.RemoveFromDeck(original, showPreview: false);
        }
        CardModel copy = ModelDb.GetById<CardModel>(cardId).ToMutable();
        for (int i = 0; i < upgradeLevel; i++)
        {
            copy.UpgradeInternal();
        }
        // Fresh cards must be registered with the run before joining a deck.
        receiver.RunState.AddCard(copy, receiver);
        await CardPileCmd.Add(copy, receiver.Deck, skipVisuals: true);
    }
}
