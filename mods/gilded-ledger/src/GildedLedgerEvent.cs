using BaseLib.Abstracts;
using BaseLib.Utils.Attributes;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace GildedLedger;

/// <summary>
/// Question-mark event: pay every coin for a free choice of enchantment on a card,
/// or remove any number of cards from the deck. Appears in all acts (shared event pool).
/// </summary>
[CustomID("GILDED_LEDGER")]
public sealed class GildedLedgerEvent : CustomEventModel
{
    /// <summary>Empty Acts → BaseLib SharedCustomEvents → rolled in every act.</summary>
    public override ActModel[] Acts => Array.Empty<ActModel>();

    /// <summary>Reuse Self-Help Book portrait (enchantment-themed) until custom art exists.</summary>
    public override string? CustomInitialPortraitPath =>
        ImageHelper.GetImagePath("events/self_help_book.png");

    public override List<(string, string)>? Localization => new EventLoc(
        "The Gilded Ledger",
        new EventPageLoc(
            "INITIAL",
            "A brass ledger floats open in the dark. Its ink is still wet — and hungry for coin. "
            + "It offers a bargain: gild any card with any enchantment if you empty your purse, "
            + "or shed any dead weight from your deck.",
            new EventOptionLoc(
                "GILD",
                "Gild a card",
                "Lose *all your gold*. Choose any enchantment, then a card to receive it."),
            new EventOptionLoc(
                "SHED",
                "Remove cards",
                "Remove any number of cards from your deck."),
            new EventOptionLoc(
                "GILD_LOCKED",
                "Gild a card (locked)",
                "You need gold and an enchantable card."),
            new EventOptionLoc(
                "SHED_LOCKED",
                "Remove cards (locked)",
                "You need at least 1 removable card.")),
        new EventPageLoc(
            "CHOOSE_ENCHANT",
            "Which enchantment should the ledger bind? (You will choose a card next, then lose all gold.)"),
        new EventPageLoc(
            "GILD_DONE",
            "The ledger drinks every coin. Your card shines with new ink."),
        new EventPageLoc(
            "SHED_DONE",
            "Pages tear free. The deck feels lighter."),
        new EventPageLoc(
            "GILD_CANCELLED",
            "You close the ledger without writing a name. The gold stays — for now."));

    public override bool IsAllowed(IRunState runState)
    {
        // Event only appears if every player could take at least one option.
        return runState.Players.All(p => CanGild(p) || CanShed(p));
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        Player owner = Owner!;
        var options = new List<EventOption>(2);

        if (CanGild(owner))
        {
            options.Add(Option(Gild, "INITIAL"));
        }
        else
        {
            options.Add(LockedOption("GILD_LOCKED"));
        }

        if (CanShed(owner))
        {
            options.Add(Option(Shed, "INITIAL"));
        }
        else
        {
            options.Add(LockedOption("SHED_LOCKED"));
        }

        return options;
    }

    /// <summary>Open a page of every enchantment that can hit at least one deck card.</summary>
    public async Task Gild()
    {
        Player owner = Owner!;
        List<EnchantmentModel> choices = GetApplicableEnchantments(owner).ToList();
        if (choices.Count == 0)
        {
            SetEventFinished(PageDescription("GILD_CANCELLED"));
            return;
        }

        // Many enchants → event options list scrolls (see EventOptionsScrollPatch).
        var options = new List<EventOption>(choices.Count);
        foreach (EnchantmentModel ench in choices)
        {
            EnchantmentModel captured = ench;
            LocString title = captured.Title;
            LocString desc = L10NLookup(Id.Entry + ".pages.INITIAL.options.GILD.description");
            options.Add(Option(
                () => ApplyGild(captured),
                title,
                desc,
                captured.HoverTip));
        }

        SetEventState(PageDescription("CHOOSE_ENCHANT"), options);
        await Task.CompletedTask;
    }

    public async Task ApplyGild(EnchantmentModel enchantment)
    {
        Player owner = Owner!;
        int amount = EnchantAmount(enchantment);
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1)
        {
            // Allow backing out of the card pick without losing gold.
            Cancelable = true,
        };

        CardModel? card = (await CardSelectCmd.FromDeckForEnchantment(
            owner, enchantment, amount, prefs)).FirstOrDefault();

        if (card == null)
        {
            // Re-open enchantment list so the player can pick another.
            await Gild();
            return;
        }

        CardCmd.Enchant(enchantment.ToMutable(), card, amount);
        PlayEnchantVfx(card);

        int gold = owner.Gold;
        if (gold > 0)
        {
            await PlayerCmd.LoseGold(gold, owner, GoldLossType.Spent);
            try
            {
                RunManager.Instance?.RewardSynchronizer?.SyncLocalGoldLost(gold);
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"Gold sync after gild failed: {e.Message}");
            }
        }

        SetEventFinished(PageDescription("GILD_DONE"));
    }

    public async Task Shed()
    {
        Player owner = Owner!;
        int removable = CountRemovable(owner);
        if (removable <= 0)
        {
            SetEventState(InitialDescription, GenerateInitialOptions());
            return;
        }

        // min 1, max all removable — player picks any count in that range (or cancels).
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1, removable)
        {
            Cancelable = true,
        };

        List<CardModel> cards = (await CardSelectCmd.FromDeckForRemoval(owner, prefs)).ToList();
        if (cards.Count == 0)
        {
            // Backed out — leave options available by regenerating initial state.
            SetEventState(InitialDescription, GenerateInitialOptions());
            return;
        }

        await CardPileCmd.RemoveFromDeck(cards);
        SetEventFinished(PageDescription("SHED_DONE"));
    }

    // ------------------------------------------------------------ helpers

    public static bool CanGild(Player player)
    {
        if (player.Gold <= 0)
        {
            return false;
        }
        return GetApplicableEnchantments(player).Any();
    }

    public static bool CanShed(Player player) => CountRemovable(player) >= 1;

    public static int CountRemovable(Player player) =>
        PileType.Deck.GetPile(player).Cards.Count(c => c.IsRemovable);

    public static IEnumerable<EnchantmentModel> GetApplicableEnchantments(Player player)
    {
        IReadOnlyList<CardModel> deck = PileType.Deck.GetPile(player).Cards;
        return ModelDb.DebugEnchantments
            .Where(IsPlayerFacingEnchantment)
            .Where(e => deck.Any(e.CanEnchant))
            .OrderBy(e => e.Title.GetFormattedText(), StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsPlayerFacingEnchantment(EnchantmentModel ench)
    {
        Type t = ench.GetType();
        if (t.IsAbstract)
        {
            return false;
        }
        string name = t.Name;
        if (name.Contains("Mock", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Deprecated", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Missing", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        string? ns = t.Namespace;
        if (ns != null && ns.Contains("Mocks", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        // Canonical instances only (ModelDb returns those).
        return true;
    }

    /// <summary>
    /// Stackable enchants (Sharp, Nimble, …) get +1; non-amount enchants still pass 1.
    /// Paying all gold should feel strong without matching Self-Help Book's free +2.
    /// </summary>
    public static int EnchantAmount(EnchantmentModel ench) => 1;

    public static void PlayEnchantVfx(CardModel card)
    {
        try
        {
            NCardEnchantVfx? vfx = NCardEnchantVfx.Create(card);
            if (vfx != null)
            {
                Node? container = NRun.Instance?.GlobalUi.CardPreviewContainer;
                if (container != null)
                {
                    container.AddChildSafely(vfx);
                }
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Enchant VFX skipped: {e.Message}");
        }
    }
}
