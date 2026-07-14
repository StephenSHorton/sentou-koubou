using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;

namespace CardRanks;

/// <summary>
/// Campfire "Combine" action. Free by default (does not spend rest action);
/// multiplayer-safe via CombineSynchronizer.
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
            return remoteOk && CardRanksConfig.SpendCampfireAction;
        }

        bool combined = await sync.RunLocalCampfireCombine(Owner);
        sync.BroadcastCampfireResult(combined);
        return combined && CardRanksConfig.SpendCampfireAction;
    }
}
