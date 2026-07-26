using MegaCrit.Sts2.Core.Localization;

namespace BattleDraw;

public static class Loc
{
    private static readonly Dictionary<string, string> SettingsEntries = new()
    {
        ["BATTLEDRAW.mod_title"] = "Battle Draw",
        ["BATTLEDRAW-BRUSH_SIZE.title"] =
            "Brush size (1–24 px) — combat doodles and your map pen",
        ["BATTLEDRAW-COLOR_PRESET.title"] =
            "Brush color preset — combat doodles and your map pen (local view)",
    };

    public static void EnsureSettingsEntries()
    {
        LocManager? mgr = LocManager.Instance;
        if (mgr == null)
            return;
        if (!mgr._tables.TryGetValue("settings_ui", out LocTable? table))
            return;

        var inject = new Dictionary<string, string>();
        foreach ((string key, string value) in SettingsEntries)
        {
            if (!table.HasEntry(key))
                inject[key] = value;
        }
        if (inject.Count > 0)
            table.MergeWith(inject);
    }
}
