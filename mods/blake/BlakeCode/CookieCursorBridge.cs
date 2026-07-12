using Godot;
using HarmonyLib;

namespace Blake.BlakeCode;

/// <summary>
/// Registers Blake with the CookieCursor workshop mod when present.
/// </summary>
public static class CookieCursorBridge
{
    private const string CharacterKey = "blake";
    private const string CookiePath = "res://Blake/images/relics/yummy_cookie_blake.png";

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

            configs[CharacterKey] = (CookiePath, 5f, new Vector2(14f, 3f));
            MainFile.Logger.Info($"Registered CookieCursor entry '{CharacterKey}' → {CookiePath}");
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Info($"CookieCursor bridge failed (non-fatal): {ex.Message}");
        }
    }
}
