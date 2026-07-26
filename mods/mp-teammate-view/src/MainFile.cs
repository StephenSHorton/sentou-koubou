using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MpTeammateView.Data;
using MpTeammateView.Settings;
using MpTeammateView.Utils;
using STS2RitsuLib.RuntimeInput;
using static MpTeammateView.Settings.ModSettingsLocalization;
using ModSettings = MpTeammateView.Data.Models.ModSettings;

namespace MpTeammateView;

/// <summary>
/// Combined multiplayer teammate UI: potions + hand cards next to the player list.
/// Based on BAKAOLC/OLC's MultiPlayerPotionView and ShowPlayerHandCards (MIT),
/// rewritten with reliable hand attach + full settings/hotkeys/highlights/interop.
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public static Logger Logger { get; } = new(Const.ModId, LogType.Generic);

    private static IRuntimeHotkeyHandle? _toggleHotkeyHandle;

    public static void Initialize()
    {
        try
        {
            ModDataStore.Initialize();
            ModSettingsBootstrap.Initialize();
            ApplyRuntimeHotkeysFromSettings();

            var harmony = new Harmony(Const.ModId);
            harmony.PatchAll();

            Logger.Info(
                $"MP Teammate View v{Const.Version} loaded — potions + hands, settings, hotkeys, highlights, LemonSpire/Typing interop. " +
                "Disable Workshop MultiPlayerPotionView / ShowPlayerHandCards if both are enabled.");
        }
        catch (Exception ex)
        {
            Logger.Error($"MP Teammate View init failed: {ex}");
            throw;
        }
    }

    internal static void ApplyRuntimeHotkeysFromSettings()
    {
        var settings = ModDataStore.Get<ModSettings>(ModDataStore.SettingsKey);
        var originalBinding = settings.ToggleKey;
        var normalizedBinding =
            RuntimeHotkeyService.NormalizeOrDefault(originalBinding, InputHandler.DefaultToggleBinding);
        if (!string.Equals(originalBinding, normalizedBinding, StringComparison.Ordinal))
        {
            ModDataStore.Modify<ModSettings>(ModDataStore.SettingsKey, s => s.ToggleKey = normalizedBinding);
            ModDataStore.Save(ModDataStore.SettingsKey);
            Logger.Warn($"Invalid toggle key '{originalBinding}', fallback to '{normalizedBinding}'.");
        }

        if (_toggleHotkeyHandle == null)
        {
            _toggleHotkeyHandle = RuntimeHotkeyService.Register(normalizedBinding, ToggleHandCardDisplay,
                new()
                {
                    Id = "mp-teammate-view.toggle-hand-display",
                    DisplayName = T("runtimeHotkey.toggle.displayName", "Toggle hand card display"),
                    Description = T("runtimeHotkey.toggle.description",
                        "Shows or hides the teammate hand card overlay."),
                    Purpose = "toggle-overlay",
                    Category = T("runtimeHotkey.category.gameplay", "Gameplay"),
                    DebugName = "mp-teammate-view.toggle",
                });
        }
        else if (!_toggleHotkeyHandle.TryRebind(normalizedBinding, out _))
        {
            _toggleHotkeyHandle.Dispose();
            _toggleHotkeyHandle = RuntimeHotkeyService.Register(normalizedBinding, ToggleHandCardDisplay,
                new()
                {
                    Id = "mp-teammate-view.toggle-hand-display",
                    DisplayName = T("runtimeHotkey.toggle.displayName", "Toggle hand card display"),
                    Description = T("runtimeHotkey.toggle.description",
                        "Shows or hides the teammate hand card overlay."),
                    Purpose = "toggle-overlay",
                    Category = T("runtimeHotkey.category.gameplay", "Gameplay"),
                    DebugName = "mp-teammate-view.toggle",
                });
        }

        Logger.Info($"Press '{normalizedBinding}' to toggle hand card display");
    }

    private static void ToggleHandCardDisplay()
    {
        TeammateViewHost.ToggleHandsVisibility();
        Logger.Info($"Hand card display toggled: {(TeammateViewHost.HandsHidden ? "Hidden" : "Visible")}");
    }
}
