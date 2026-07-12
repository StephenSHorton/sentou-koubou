using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace Whitney.WhitneyCode;

/// <summary>
/// Registers Whitney with the CookieCursor workshop mod (id 3749027556).
/// CookieCursor keys characters by <c>GetType().Name.ToLower()</c> and loads
/// <c>yummy_cookie_*.png</c> relic-style icons as cursors.
/// </summary>
public static class CookieCursorBridge
{
    private const string CharacterKey = "whitney";
    private const string CookiePath = "res://Whitney/images/relics/yummy_cookie_whitney.png";

    public static void TryRegister()
    {
        try
        {
            var coreType = AccessTools.TypeByName("CookieCursor.Core");
            if (coreType is null)
                return;

            var field = AccessTools.Field(coreType, "CookieConfigs");
            if (field?.GetValue(null) is not System.Collections.IDictionary configs)
                return;

            if (configs.Contains(CharacterKey))
                return;

            configs[CharacterKey] = (CookiePath, 5f, new Vector2(12f, 4f));
            MainFile.Logger.Info($"Registered CookieCursor entry '{CharacterKey}' → {CookiePath}");
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Info($"CookieCursor bridge failed (non-fatal): {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.Launch))]
public static class CookieCursorBridgeLaunchPatch
{
    private static void Prefix() => CookieCursorBridge.TryRegister();
}
