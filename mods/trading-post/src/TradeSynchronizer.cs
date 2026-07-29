using System.Collections.Concurrent;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
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
/// Gold gifts are unlimited at shops; card gifts happen at campfires (rest action).
/// Shop sells (potion / relic → merchant gold) are also synced so inventories match.
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
        messageBuffer.RegisterMessageHandler<SellPotionMessage>(HandleSellPotion);
        messageBuffer.RegisterMessageHandler<SellRelicMessage>(HandleSellRelic);
    }

    public void Dispose()
    {
        _messageBuffer.UnregisterMessageHandler<GiftGoldMessage>(HandleGiftGold);
        _messageBuffer.UnregisterMessageHandler<GiftCardMessage>(HandleGiftCard);
        _messageBuffer.UnregisterMessageHandler<CampfireTradeResultMessage>(HandleCampfireResult);
        _messageBuffer.UnregisterMessageHandler<SellPotionMessage>(HandleSellPotion);
        _messageBuffer.UnregisterMessageHandler<SellRelicMessage>(HandleSellRelic);
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
        // Drive the deck-select screen directly instead of CardSelectCmd.FromDeckGeneric:
        // the Cmd reserves an id from PlayerChoiceSynchronizer, which only ticks on the
        // giver's client (mirrors never run this flow) and desyncs the run checksum.
        // The trade itself is synced by GiftCardMessage, so no synced choice is needed.
        CardModel? card = (await PickCardFromLocalDeck(prefs))?.FirstOrDefault();
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

    /// <summary>UI-only deck picker; never touches the synced choice-id stream.</summary>
    private async Task<IEnumerable<CardModel>?> PickCardFromLocalDeck(CardSelectorPrefs prefs)
    {
        List<CardModel> cards = LocalPlayer.Deck.Cards.ToList();
        if (cards.Count == 0)
        {
            return null;
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
            return null;
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

    // ---------------------------------------------------------------- shop sells (merchant)

    /// <summary>Local player sells a belt potion to the merchant for gold.</summary>
    public async Task SellPotionLocal(PotionModel potion)
    {
        if (!MerchantContext.IsInShop())
        {
            TradeUi.Notify("The merchant only buys potions at the shop.");
            return;
        }
        if (potion.Owner != LocalPlayer || potion.HasBeenRemovedFromState)
        {
            return;
        }
        int gold = SellPricing.PotionSellPrice(potion);
        ModelId id = potion.Id;
        try
        {
            await ApplyPotionSale(LocalPlayer, id, gold);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Potion sale failed: {e}");
            TradeUi.Notify("The sale fizzled — nothing was exchanged.");
            return;
        }
        _gameService.SendMessage(new SellPotionMessage
        {
            category = id.Category,
            entry = id.Entry,
            gold = gold,
            Location = _messageBuffer.CurrentLocation
        });
        TradeUi.Notify($"Sold for {gold} gold.");
    }

    private void HandleSellPotion(SellPotionMessage message, ulong senderId)
    {
        Player seller = _playerCollection.GetPlayer(senderId);
        var id = new ModelId(message.category, message.entry);
        TaskHelper.RunSafely(ApplyPotionSale(seller, id, message.gold));
    }

    private static async Task ApplyPotionSale(Player seller, ModelId potionId, int gold)
    {
        PotionModel? potion = seller.Potions.FirstOrDefault(p => p.Id == potionId && !p.HasBeenRemovedFromState);
        if (potion == null)
        {
            MainFile.Logger.Warn($"Sell potion: no matching potion {potionId} on {seller.NetId}");
            return;
        }
        await PotionCmd.Discard(potion);
        if (gold > 0)
        {
            await PlayerCmd.GainGold(gold, seller);
        }
    }

    /// <summary>Local player sells a tradable relic to the merchant for gold.</summary>
    public async Task SellRelicLocal(RelicModel relic)
    {
        if (!MerchantContext.IsInShop())
        {
            TradeUi.Notify("The merchant only buys relics at the shop.");
            return;
        }
        if (!SellPricing.CanSellRelic(relic) || relic.Owner != LocalPlayer)
        {
            TradeUi.Notify("That relic can't be sold.");
            return;
        }
        int gold = SellPricing.RelicSellPrice(relic);
        ModelId id = relic.Id;
        try
        {
            await ApplyRelicSale(LocalPlayer, id, gold);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Relic sale failed: {e}");
            TradeUi.Notify("The sale fizzled — nothing was exchanged.");
            return;
        }
        _gameService.SendMessage(new SellRelicMessage
        {
            category = id.Category,
            entry = id.Entry,
            gold = gold,
            Location = _messageBuffer.CurrentLocation
        });
        TradeUi.Notify($"Sold for {gold} gold.");
    }

    private void HandleSellRelic(SellRelicMessage message, ulong senderId)
    {
        Player seller = _playerCollection.GetPlayer(senderId);
        var id = new ModelId(message.category, message.entry);
        TaskHelper.RunSafely(ApplyRelicSale(seller, id, message.gold));
    }

    private static async Task ApplyRelicSale(Player seller, ModelId relicId, int gold)
    {
        RelicModel? relic = seller.Relics.FirstOrDefault(r => r.Id == relicId && !r.HasBeenRemovedFromState);
        if (relic == null)
        {
            MainFile.Logger.Warn($"Sell relic: no matching relic {relicId} on {seller.NetId}");
            return;
        }
        // Undo pickup bonuses (max HP, potion slots) while Owner is still valid.
        await RelicSellEffects.RevertPermanentEffects(relic);
        await RelicCmd.Remove(relic);
        if (gold > 0)
        {
            await PlayerCmd.GainGold(gold, seller);
        }
    }
}
