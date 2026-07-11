using MegaCrit.Sts2.Core.Localization;

namespace TradingPost;

/// <summary>
/// Runtime-injected localization table so this mod needs no .pck. Re-injected lazily
/// because the game rebuilds its table dictionary on locale change.
/// </summary>
public static class Loc
{
    private const string Table = "trading_post";

    private static readonly Dictionary<string, string> _entries = new()
    {
        ["GIVE_CARD"] = "Choose a card to give away.",
    };

    public static LocString Get(string key)
    {
        LocManager mgr = LocManager.Instance;
        if (!mgr._tables.ContainsKey(Table))
        {
            mgr._tables[Table] = new LocTable(Table, new Dictionary<string, string>(_entries));
        }
        return new LocString(Table, key);
    }
}
