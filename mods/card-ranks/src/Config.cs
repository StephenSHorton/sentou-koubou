using BaseLib.Config;

namespace CardRanks;

/// <summary>
/// Instance-backed settings (BaseLib SimpleModConfig). No static gameplay flags —
/// RankUpCards2's statics defaulted false until UI thrashing, which broke early runs.
/// </summary>
public sealed class CardRanksConfig : SimpleModConfig
{
    /// <summary>
    /// When false, basic Strike/Defend-like cards cannot be combined
    /// (vanilla IsBasicStrikeOrDefend and modded Basic+Strike/Defend tags).
    /// </summary>
    public bool AllowCombineStrikeDefend { get; set; } = false;

    /// <summary>
    /// When true, a successful combine spends the campfire action.
    /// Default false matches RankUpCards2's free Combine tile.
    /// </summary>
    public bool SpendCampfireAction { get; set; } = false;
}
