using BaseLib.Config;

namespace CardRanks;

/// <summary>
/// BaseLib SimpleModConfig only persists and lists <b>static</b> properties
/// (instance get/set are logged as ignored and the mod is omitted from the config menu).
/// Defaults are the source of truth until Load() overwrites from disk.
/// </summary>
public sealed class CardRanksConfig : SimpleModConfig
{
    /// <summary>
    /// When false, basic Strike/Defend-like cards cannot be combined
    /// (vanilla IsBasicStrikeOrDefend and modded Basic+Strike/Defend tags).
    /// Default true so starter decks can combine from the first campfire.
    /// </summary>
    public static bool AllowCombineStrikeDefend { get; set; } = true;

    /// <summary>
    /// When true, a successful combine spends the campfire action (default).
    /// Turn off only for free-combine testing / special runs.
    /// </summary>
    public static bool SpendCampfireAction { get; set; } = true;

    /// <summary>
    /// When true, reaching Tier I, II, or III auto-grants a random bonus enchantment.
    /// Bonuses never clear the card's rank.
    /// </summary>
    public static bool OfferTierBonusRolls { get; set; } = true;
}
