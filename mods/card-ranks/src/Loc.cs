using MegaCrit.Sts2.Core.Localization;

namespace CardRanks;

public static class Loc
{
    private const string Table = "card_ranks";

    private static readonly Dictionary<string, string> Entries = new()
    {
        ["TO_COMBINE"] =
            "Choose [blue]3[/blue] matching cards to combine (keep 1, sacrifice 2) and raise [gold]tier[/gold]",
        ["CARDRANKS.mod_title"] = "Card Ranks",
        ["CARDRANKS-ALLOW_COMBINE_STRIKE_DEFEND.title"] =
            "Allow combining Strike and Defend (including modded Basic Strike/Defend)",
        ["CARDRANKS-SPEND_CAMPFIRE_ACTION.title"] =
            "Spend campfire action when combining (default on; off = free combine)",
        ["CARDRANKS-OFFER_TIER_BONUS_ROLLS.title"] =
            "Auto-grant a random bonus enchantment on Tier II and III (not on first tier)",
    };

    private static readonly Dictionary<string, string> EnchantmentEntries = new()
    {
        ["CARDRANKS-FIRST_RANK.title"] = "Tier I",
        ["CARDRANKS-FIRST_RANK.description"] =
            "[blue]Tier I[/blue]. Effect ×[blue]1.5[/blue] damage and block.",
        ["CARDRANKS-SECOND_RANK.title"] = "Tier II",
        ["CARDRANKS-SECOND_RANK.description"] =
            "[purple]Tier II[/purple]. Effect ×[blue]2[/blue] damage and block.",
        ["CARDRANKS-THIRD_RANK.title"] = "Tier III",
        ["CARDRANKS-THIRD_RANK.description"] =
            "[gold]Tier III[/gold]. Effect ×[blue]3[/blue] damage and block.",
    };

    private static int _dynCounter;

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

    public static LocString Dynamic(string text)
    {
        LocTable table = EnsureTable();
        string key = "DYN_" + _dynCounter++ % 64;
        table.MergeWith(new Dictionary<string, string>
        {
            [key] = text.Replace('{', '(').Replace('}', ')'),
        });
        return new LocString(Table, key);
    }

    public static void EnsureRestSiteEntries()
    {
        LocManager mgr = LocManager.Instance;
        if (!mgr._tables.TryGetValue("rest_site_ui", out LocTable? table))
            return;
        var inject = new Dictionary<string, string>
        {
            ["OPTION_COMBINE_RANK.name"] = "Combine",
            ["OPTION_COMBINE_RANK.description"] =
                "Combine [blue]3[/blue] identical same-tier cards (keep 1): [blue]Tier I[/blue] → II → III (×1.5 / ×2 / ×3). Spends rest. Bonus enchantment on Tier II and III only.",
            ["OPTION_COMBINE_RANK.descriptionDisabled"] =
                "[red]Need 3 matching cards of the same tier to combine.[/red]",
            ["OPTION_COMBINE_RANK.descriptionBasicsBlocked"] =
                "[red]Strike/Defend are blocked by mod settings.[/red] Enable [gold]Allow combining Strike and Defend[/gold] in Card Ranks options.",
        };
        // Clone uses the vanilla rest option (OptionId CLONE) — no custom loc.
        table.MergeWith(inject);
    }

    public static void EnsureSettingsEntries()
    {
        LocManager? mgr = LocManager.Instance;
        if (mgr == null)
            return;
        if (!mgr._tables.TryGetValue("settings_ui", out LocTable? table))
            return;
        var inject = new Dictionary<string, string>();
        foreach ((string key, string value) in Entries)
        {
            bool isSettingsKey = key.EndsWith(".title", StringComparison.Ordinal)
                                 || key.EndsWith(".mod_title", StringComparison.Ordinal)
                                 || key.Contains(".mod_title", StringComparison.Ordinal);
            if (isSettingsKey && !table.HasEntry(key))
                inject[key] = value;
        }
        if (!table.HasEntry("CARDRANKS.mod_title"))
            inject["CARDRANKS.mod_title"] = "Card Ranks";
        if (inject.Count > 0)
            table.MergeWith(inject);
    }

    public static void EnsureCardSelectionEntries()
    {
        LocManager mgr = LocManager.Instance;
        if (!mgr._tables.TryGetValue("card_selection", out LocTable? table))
            return;
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
