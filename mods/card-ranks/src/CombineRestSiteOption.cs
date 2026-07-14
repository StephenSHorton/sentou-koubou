using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;

namespace CardRanks;

/// <summary>
/// Campfire "Combine" action. Spends the rest-site action on success by default
/// (config <see cref="CardRanksConfig.SpendCampfireAction"/>). Multiplayer-safe
/// via CombineSynchronizer.
/// </summary>
public sealed class CombineRestSiteOption : RestSiteOption
{
    public const string Id = "COMBINE_RANK";

    public override string OptionId => Id;

    public override IEnumerable<string> AssetPaths => Enumerable.Empty<string>();

    public override bool IsEnabled => CombineService.DeckHasCombinablePair(Owner);

    public override LocString Description
    {
        get
        {
            Loc.EnsureRestSiteEntries();
            if (IsEnabled)
                return new LocString("rest_site_ui", $"OPTION_{OptionId}.description");
            if (CombineService.OnlyBlockedByBasicsPolicy(Owner))
                return new LocString("rest_site_ui", $"OPTION_{OptionId}.descriptionBasicsBlocked");
            return new LocString("rest_site_ui", $"OPTION_{OptionId}.descriptionDisabled");
        }
    }

    public CombineRestSiteOption(Player owner) : base(owner)
    {
    }

    public override async Task<bool> OnSelect()
    {
        CombineSynchronizer? sync = CombineSynchronizer.Instance;
        if (sync == null)
        {
            MainFile.Logger.Warn("CombineSynchronizer missing; combine unavailable.");
            return false;
        }

        if (!LocalContext.IsMe(Owner))
        {
            bool remoteOk = await sync.AwaitCampfireResult(Owner.NetId);
            return remoteOk && ShouldSpendRestAction();
        }

        bool combined = await sync.RunLocalCampfireCombine(Owner);
        sync.BroadcastCampfireResult(combined);

        // Free-combine mode only: keep rest site open and refresh chrome.
        if (combined && !ShouldSpendRestAction())
            RestSiteUi.RefreshAfterCombine(this);

        return combined && ShouldSpendRestAction();
    }

    /// <summary>Spend rest unless config disables it (or a free-rest relic is present).</summary>
    private bool ShouldSpendRestAction()
    {
        if (!CardRanksConfig.SpendCampfireAction)
            return false;
        // Girya / similar extra-campfire relics still use a spent action per pick;
        // no free-combine exemption unless we add an explicit relic later.
        return true;
    }
}
