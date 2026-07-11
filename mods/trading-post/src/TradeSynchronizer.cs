using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace TradingPost;

/// <summary>
/// Synchronizes shop trades between co-op players, modeled on the game's OneOffSynchronizer.
/// The initiating client applies the trade locally and broadcasts a message; every other
/// client mirrors the same state change for the involved players.
/// </summary>
public class TradeSynchronizer : IDisposable
{
    public static TradeSynchronizer? Instance { get; set; }

    private readonly RunLocationTargetedMessageBuffer _messageBuffer;

    private readonly INetGameService _gameService;

    private readonly IPlayerCollection _playerCollection;

    private readonly ulong _localPlayerId;

    /// <summary>The local player has used their one trade for this shop visit.</summary>
    public bool LocalTradeUsed { get; private set; }

    /// <summary>A relic request is in flight; block further trade attempts until answered.</summary>
    public bool RelicRequestPending { get; private set; }

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
        messageBuffer.RegisterMessageHandler<RelicRequestMessage>(HandleRelicRequest);
        messageBuffer.RegisterMessageHandler<RelicResponseMessage>(HandleRelicResponse);
    }

    public void Dispose()
    {
        _messageBuffer.UnregisterMessageHandler<GiftGoldMessage>(HandleGiftGold);
        _messageBuffer.UnregisterMessageHandler<GiftCardMessage>(HandleGiftCard);
        _messageBuffer.UnregisterMessageHandler<RelicRequestMessage>(HandleRelicRequest);
        _messageBuffer.UnregisterMessageHandler<RelicResponseMessage>(HandleRelicResponse);
    }

    /// <summary>Called each time a merchant room loads; every shop visit grants a fresh trade.</summary>
    public void ResetVisit()
    {
        LocalTradeUsed = false;
        RelicRequestPending = false;
    }

    public IReadOnlyList<Player> OtherPlayers =>
        _playerCollection.Players.Where(p => p.NetId != _localPlayerId).ToList();

    public static string NameOf(Player player)
    {
        return PlatformUtil.GetPlayerNameRaw(RunManager.Instance.NetService.Platform, player.NetId);
    }

    // ---------------------------------------------------------------- gold

    /// <summary>Local player gifts gold to another player. Free — this is pure generosity.</summary>
    public async Task GiftGoldLocal(Player target, int amount)
    {
        amount = Math.Clamp(amount, 0, LocalPlayer.Gold);
        if (amount <= 0)
        {
            return;
        }
        LocalTradeUsed = true;
        _gameService.SendMessage(new GiftGoldMessage
        {
            targetNetId = target.NetId,
            amount = amount,
            Location = _messageBuffer.CurrentLocation
        });
        await ApplyGoldGift(LocalPlayer, target, amount);
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

    // ---------------------------------------------------------------- cards

    /// <summary>
    /// Local player picks a card from their deck and gifts it. Free.
    /// Returns false if the player backed out of the card picker.
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
        LocalTradeUsed = true;
        _gameService.SendMessage(new GiftCardMessage
        {
            targetNetId = target.NetId,
            category = card.Id.Category,
            entry = card.Id.Entry,
            upgradeLevel = card.CurrentUpgradeLevel,
            Location = _messageBuffer.CurrentLocation
        });
        await ApplyCardGift(LocalPlayer, target, card.Id, card.CurrentUpgradeLevel);
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
        copy.Owner = receiver;
        for (int i = 0; i < upgradeLevel; i++)
        {
            copy.UpgradeInternal();
        }
        await CardPileCmd.Add(copy, receiver.Deck, skipVisuals: true);
    }

    // ---------------------------------------------------------------- relics

    /// <summary>
    /// Local player browses the target's relics and asks for one, offering ALL their gold.
    /// The target must accept before anything changes hands.
    /// Returns false if the player backed out of the relic picker.
    /// </summary>
    public async Task<bool> RequestRelicLocal(Player target)
    {
        RelicModel? relic = await RelicSelectCmd.FromChooseARelicScreen(LocalPlayer, target.Relics.ToList());
        if (relic == null)
        {
            return false;
        }
        LocalTradeUsed = true;
        RelicRequestPending = true;
        _gameService.SendMessage(new RelicRequestMessage
        {
            targetNetId = target.NetId,
            category = relic.Id.Category,
            entry = relic.Id.Entry,
            Location = _messageBuffer.CurrentLocation
        });
        TradeUi.Notify($"Offer sent to {NameOf(target)} — waiting for their answer…");
        return true;
    }

    private void HandleRelicRequest(RelicRequestMessage message, ulong senderId)
    {
        // Only the relic's owner reacts here; everyone else waits for the response message.
        if (message.targetNetId != _localPlayerId)
        {
            return;
        }
        Player requester = _playerCollection.GetPlayer(senderId);
        var id = new ModelId(message.category, message.entry);
        string relicName = ModelDb.GetByIdOrNull<RelicModel>(id)?.Title.GetFormattedText() ?? message.entry;
        TradeUi.Confirm(
            $"{NameOf(requester)} offers ALL of their gold ({requester.Gold}) for your {relicName}. Hand it over?",
            accepted =>
            {
                _gameService.SendMessage(new RelicResponseMessage
                {
                    requesterNetId = senderId,
                    category = message.category,
                    entry = message.entry,
                    accepted = accepted,
                    Location = _messageBuffer.CurrentLocation
                });
                if (accepted)
                {
                    TaskHelper.RunSafely(ApplyRelicTrade(LocalPlayer, requester, id));
                }
            });
    }

    private void HandleRelicResponse(RelicResponseMessage message, ulong senderId)
    {
        Player giver = _playerCollection.GetPlayer(senderId);
        Player requester = _playerCollection.GetPlayer(message.requesterNetId);
        var id = new ModelId(message.category, message.entry);
        if (!message.accepted)
        {
            if (requester == LocalPlayer)
            {
                // Declined: give the trade back — nothing changed hands.
                RelicRequestPending = false;
                LocalTradeUsed = false;
                TradeUi.Notify($"{NameOf(giver)} declined your offer.");
            }
            return;
        }
        if (requester == LocalPlayer)
        {
            RelicRequestPending = false;
            TradeUi.Notify($"{NameOf(giver)} accepted! The relic is yours.");
        }
        TaskHelper.RunSafely(ApplyRelicTrade(giver, requester, id));
    }

    private static async Task ApplyRelicTrade(Player giver, Player requester, ModelId relicId)
    {
        RelicModel? relic = giver.Relics.FirstOrDefault(r => r.Id == relicId);
        if (relic != null)
        {
            await RelicCmd.Remove(relic);
        }
        // The requester pays with everything they've got; the gold is burned, not transferred.
        if (requester.Gold > 0)
        {
            await PlayerCmd.LoseGold(requester.Gold, requester, GoldLossType.Spent);
        }
        await RelicCmd.Obtain(ModelDb.GetById<RelicModel>(relicId).ToMutable(), requester);
    }
}
