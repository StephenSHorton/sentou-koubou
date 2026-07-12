using MegaCrit.Sts2.Core.Localization;

namespace TradingPost;

/// <summary>
/// Runtime-injected localization table so this mod needs no .pck. Re-injected lazily
/// because the game rebuilds its table dictionary on locale change. Dynamic entries
/// let free-form runtime text flow through APIs that require a LocString.
/// </summary>
public static class Loc
{
    private const string Table = "trading_post";

    private static int _dynCounter;

    private static readonly Dictionary<string, string> _entries = new()
    {
        ["TITLE"] = "Trading Post",
        ["OK"] = "Okay",
        ["ACCEPT"] = "Accept",
        ["DECLINE"] = "Decline",
        ["GIVE_CARD"] = "Choose a card to give away.",
    };

    private static LocTable EnsureTable()
    {
        LocManager mgr = LocManager.Instance;
        if (!mgr._tables.TryGetValue(Table, out LocTable? table))
        {
            table = new LocTable(Table, new Dictionary<string, string>(_entries));
            mgr._tables[Table] = table;
        }
        return table;
    }

    public static LocString Get(string key)
    {
        EnsureTable();
        return new LocString(Table, key);
    }

    /// <summary>Injects the campfire Trade tile's name/description into the game's rest-site table.</summary>
    public static void EnsureRestSiteEntries()
    {
        LocManager mgr = LocManager.Instance;
        if (mgr._tables.TryGetValue("rest_site_ui", out LocTable? table) && !table.HasEntry("OPTION_TRADE.name"))
        {
            table.MergeWith(new Dictionary<string, string>
            {
                ["OPTION_TRADE.name"] = "Trade",
                ["OPTION_TRADE.description"] = "Give a card from your deck to a fellow climber. Spends your time at the campfire.",
            });
        }
    }

    /// <summary>Wraps free-form runtime text in a LocString via a rotating dynamic key.</summary>
    public static LocString Dynamic(string text)
    {
        LocTable table = EnsureTable();
        string key = "DYN_" + _dynCounter++ % 64;
        // Braces would be parsed as SmartFormat placeholders; neutralize them.
        table.MergeWith(new Dictionary<string, string> { [key] = text.Replace('{', '(').Replace('}', ')') });
        return new LocString(Table, key);
    }
}
