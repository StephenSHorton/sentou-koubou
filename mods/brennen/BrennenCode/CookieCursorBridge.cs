using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace Brennen.BrennenCode;

/// <summary>
/// Registers Brennen with the CookieCursor workshop mod (id 3749027556).
/// CookieCursor keys characters by <c>GetType().Name.ToLower()</c> and loads
/// <c>yummy_cookie_*.png</c> relic-style icons as cursors.
/// </summary>
public static class CookieCursorBridge
{
    private const string CharacterKey = "brennen";
    private const string CookiePath = "res://Brennen/images/relics/yummy_cookie_brennen.png";

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

            // Matches CookieCursor's (string Path, float TiltAngle, Vector2 BaseHotSpot)
            configs[CharacterKey] = (CookiePath, 5f, new Vector2(14f, 3f));
            MainFile.Logger.Info($"Registered CookieCursor entry '{CharacterKey}' → {CookiePath}");
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Info($"CookieCursor bridge failed (non-fatal): {ex.Message}");
        }
    }
}

/// <summary>Re-register right before CookieCursor's Launch postfix applies cursors.</summary>
[HarmonyPatch(typeof(RunManager), nameof(RunManager.Launch))]
public static class CookieCursorBridgeLaunchPatch
{
    private static void Prefix() => CookieCursorBridge.TryRegister();
}
