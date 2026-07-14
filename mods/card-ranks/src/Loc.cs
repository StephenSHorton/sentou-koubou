using MegaCrit.Sts2.Core.Localization;

namespace CardRanks;

public static class Loc
{
    private const string Table = "card_ranks";

    private static readonly Dictionary<string, string> Entries = new()
    {
        ["TO_COMBINE"] = "Choose matching{Amount:choose(1):| [blue]{}[/blue] cards} to combine and raise their [gold]Tier[/gold]",
        ["CARDRANKS.mod_title"] = "Card Ranks",
        ["CARDRANKS-ALLOW_COMBINE_STRIKE_DEFEND.title"] =
            "Allow combining Strike and Defend (including modded Basic Strike/Defend)",
        ["CARDRANKS-SPEND_CAMPFIRE_ACTION.title"] =
            "Spend campfire action when combining (off = free action)",
        ["CARDRANKS-OFFER_TIER_BONUS_ROLLS.title"] =
            "Auto-grant a random bonus when a card reaches a new tier (I / II / III)",
    };

    private static readonly Dictionary<string, string> EnchantmentEntries = new()
    {
        ["CARDRANKS-FIRST_RANK.title"] = "Tier I",
        ["CARDRANKS-FIRST_RANK.description"] =
            "Tier I (blue). Effect multiplied by [blue]1.5[/blue] (damage and block).",
        ["CARDRANKS-SECOND_RANK.title"] = "Tier II",
        ["CARDRANKS-SECOND_RANK.description"] =
            "Tier II. Effect multiplied by [blue]2[/blue] (damage and block).",
        ["CARDRANKS-THIRD_RANK.title"] = "Tier III",
        ["CARDRANKS-THIRD_RANK.description"] =
            "Tier III. Effect multiplied by [blue]3[/blue] (damage and block).",
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

    /// <summary>Free-form runtime text for popups (SmartFormat-safe).</summary>
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
        var inject = new Dictionary<string, string>();
        if (!table.HasEntry("OPTION_COMBINE_RANK.name"))
        {
            inject["OPTION_COMBINE_RANK.name"] = "Combine";
            inject["OPTION_COMBINE_RANK.description"] =
                "Combine two identical cards: [blue]Tier I[/blue] → II → III (×1.5 / ×2 / ×3). Auto bonus each tier.";
            inject["OPTION_COMBINE_RANK.descriptionDisabled"] =
                "[red]No matching cards available to combine.[/red]";
            inject["OPTION_COMBINE_RANK.descriptionBasicsBlocked"] =
                "[red]Strike/Defend are blocked by mod settings.[/red] Enable [gold]Allow combining Strike and Defend[/gold] in Card Ranks options.";
        }
        if (!table.HasEntry("OPTION_CLONE_RANK.name"))
        {
            inject["OPTION_CLONE_RANK.name"] = "Clone";
            inject["OPTION_CLONE_RANK.description"] =
                "Duplicate a card that has the [gold]Clone[/gold] tier bonus. (Free action.)";
            inject["OPTION_CLONE_RANK.descriptionDisabled"] =
                "[red]No Clone-bonus cards in your deck.[/red]";
        }
        if (inject.Count > 0)
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
        // Also cover common prefix variants BaseLib may derive from type/namespace.
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
