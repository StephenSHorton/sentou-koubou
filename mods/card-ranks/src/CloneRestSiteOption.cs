using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace CardRanks;

/// <summary>
/// Campfire action for cards that rolled the Clone bonus: copy one such card into the deck.
/// Free action (does not spend rest) when enabled.
/// Uses <see cref="MegaCrit.Sts2.Core.Runs.ICardScope.CloneCard"/> like vanilla Clone rest option
/// — never <c>pick.ToMutable()</c> on a deck card (throws MutableModelException).
/// </summary>
public sealed class CloneRestSiteOption : RestSiteOption
{
    public const string Id = "CLONE_RANK";

    public override string OptionId => Id;

    public override IEnumerable<string> AssetPaths => Enumerable.Empty<string>();

    public static bool DeckHasCloneCard(Player player) =>
        CombineService.GetDeckCards(player).Any(TierBonusService.HasClone);

    public override bool IsEnabled => DeckHasCloneCard(Owner);

    public override LocString Description
    {
        get
        {
            Loc.EnsureRestSiteEntries();
            string key = IsEnabled
                ? $"OPTION_{OptionId}.description"
                : $"OPTION_{OptionId}.descriptionDisabled";
            return new LocString("rest_site_ui", key);
        }
    }

    public CloneRestSiteOption(Player owner) : base(owner)
    {
    }

    public override async Task<bool> OnSelect()
    {
        if (!LocalContext.IsMe(Owner) || !IsEnabled)
            return false;

        Loc.EnsureCardSelectionEntries();
        var prefs = new CardSelectorPrefs(Loc.Dynamic("Choose a Clone card to duplicate"), 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true,
        };

        CardModel? pick = (await CardSelectCmd.FromDeckGeneric(
            Owner, prefs, TierBonusService.HasClone, null)).FirstOrDefault();
        if (pick == null)
            return false;

        try
        {
            // Same path as vanilla CloneRestSiteOption:
            // RunState.CloneCard copies upgrades/enchantments into a fresh run-owned instance.
            CardModel copy = Owner.RunState.CloneCard(pick);

            // ConditionalWeakTable flags don't travel with CloneCard — re-attach them.
            TierBonusService.CopyFlagsOnly(pick, copy);
            CombineService.Track(copy, CombineService.GetRank(pick));

            var added = await CardPileCmd.Add(copy, PileType.Deck, skipVisuals: false);
            CardCmd.PreviewCardPileAdd(added, 1.5f);
            MainFile.Logger.Info(
                $"Cloned {pick.Id} at rest site → {CombineService.Describe(copy)}");
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Clone failed: {e}");
            return false;
        }

        return false; // free action
    }
}
