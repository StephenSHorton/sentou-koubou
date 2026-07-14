using MegaCrit.Sts2.Core.Localization;

namespace CardRanks;

public static class Loc
{
    private const string Table = "card_ranks";

    private static readonly Dictionary<string, string> Entries = new()
    {
        ["TO_COMBINE"] = "Choose matching{Amount:choose(1):| [blue]{}[/blue] cards} to combine and raise their [gold]Rank[/gold]",
        ["CARDRANKS-ALLOW_COMBINE_STRIKE_DEFEND.title"] =
            "Allow combining Strike and Defend (including modded Basic Strike/Defend)",
        ["CARDRANKS-SPEND_CAMPFIRE_ACTION.title"] =
            "Spend campfire action when combining (off = free action)",
    };

    private static readonly Dictionary<string, string> EnchantmentEntries = new()
    {
        ["CARDRANKS-SECOND_RANK.title"] = "Rank 2",
        ["CARDRANKS-SECOND_RANK.description"] = "The card's effect is multiplied by [blue]1.5[/blue] (damage and block).",
        ["CARDRANKS-THIRD_RANK.title"] = "Rank 3",
        ["CARDRANKS-THIRD_RANK.description"] = "The card's effect is multiplied by [blue]3[/blue] (damage and block).",
    };

    private static LocTable EnsureTable()
    {
        LocManager mgr = LocManager.Instance;
        if (!mgr._tables.TryGetValue(Table, out LocTable? table))
        {
            table = new LocTable(Table, new Dictionary<string, string>(Entries));
            mgr._tables[Table] = table;
        }
        return table;
    }

    public static LocString Get(string key)
    {
        EnsureTable();
        return new LocString(Table, key);
    }

    public static void EnsureRestSiteEntries()
    {
        LocManager mgr = LocManager.Instance;
        if (!mgr._tables.TryGetValue("rest_site_ui", out LocTable? table))
            return;
        if (table.HasEntry("OPTION_COMBINE_RANK.name"))
            return;
        table.MergeWith(new Dictionary<string, string>
        {
            ["OPTION_COMBINE_RANK.name"] = "Combine",
            ["OPTION_COMBINE_RANK.description"] =
                "Combine two identical cards to raise their [gold]Rank[/gold] (×1.5 then ×3 damage and block).",
            ["OPTION_COMBINE_RANK.descriptionDisabled"] =
                "[red]No matching cards available to combine.[/red]",
        });
    }

    public static void EnsureSettingsEntries()
    {
        LocManager mgr = LocManager.Instance;
        if (!mgr._tables.TryGetValue("settings_ui", out LocTable? table))
            return;
        var inject = new Dictionary<string, string>();
        foreach ((string key, string value) in Entries)
        {
            if (key.EndsWith(".title", StringComparison.Ordinal) && !table.HasEntry(key))
                inject[key] = value;
        }
        if (inject.Count > 0)
            table.MergeWith(inject);
    }

    public static void EnsureCardSelectionEntries()
    {
        LocManager mgr = LocManager.Instance;
        if (!mgr._tables.TryGetValue("card_selection", out LocTable? table))
            return;
        if (!table.HasEntry("TO_COMBINE"))
            table.MergeWith(new Dictionary<string, string> { ["TO_COMBINE"] = Entries["TO_COMBINE"] });
    }

    public static void EnsureEnchantmentEntries()
    {
        LocManager mgr = LocManager.Instance;
        if (!mgr._tables.TryGetValue("enchantments", out LocTable? table))
            return;
        var inject = new Dictionary<string, string>();
        foreach ((string key, string value) in EnchantmentEntries)
        {
            if (!table.HasEntry(key))
                inject[key] = value;
        }
        if (inject.Count > 0)
            table.MergeWith(inject);
    }
}
