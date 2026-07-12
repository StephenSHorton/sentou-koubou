using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace TradingPost;

/// <summary>
/// The campfire "Trade" action: gift a card from your deck to another player.
/// Success consumes the campfire action via the game's own rest-site flow; backing
/// out leaves the action unspent. OnSelect runs on every client — the owner drives
/// the UI, mirrors await the broadcast outcome.
/// </summary>
public sealed class TradeRestSiteOption : RestSiteOption
{
    public override string OptionId => "TRADE";

    // No preloadable assets; the icon comes from a Harmony patch on the Icon getter.
    public override IEnumerable<string> AssetPaths => Enumerable.Empty<string>();

    public override bool IsEnabled => (TradeSynchronizer.Instance?.OtherPlayers.Count ?? 0) > 0;

    public TradeRestSiteOption(Player owner)
        : base(owner)
    {
    }

    public override async Task<bool> OnSelect()
    {
        TradeSynchronizer? sync = TradeSynchronizer.Instance;
        if (sync == null)
        {
            return false;
        }
        if (!LocalContext.IsMe(Owner))
        {
            return await sync.AwaitCampfireResult(Owner.NetId);
        }
        return await sync.RunLocalCampfireTrade();
    }
}
