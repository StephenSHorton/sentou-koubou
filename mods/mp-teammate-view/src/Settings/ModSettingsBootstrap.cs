using Godot;
using MpTeammateView.Data;
using MpTeammateView.Data.Models;
using MpTeammateView.Utils;
using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace MpTeammateView.Settings;

internal static class ModSettingsBootstrap
{
    private static readonly Lock InitLock = new();
    private static bool _initialized;

    private static readonly string[] CardKeywordOptions =
        ["Exhaust", "Ethereal", "Innate", "Unplayable", "Retain", "Sly", "Eternal"];

    private static readonly string[] CardTypeOptions = ["Attack", "Skill", "Power", "Status", "Curse", "Quest"];

    private static readonly string[] CardRarityOptions =
        ["Basic", "Common", "Uncommon", "Rare", "Ancient", "Event", "Token", "Status", "Curse", "Quest"];

    private static readonly string[] TargetTypeOptions =
    [
        "Self", "AnyEnemy", "AllEnemies", "RandomEnemy", "AnyPlayer", "AnyAlly", "AllAllies", "TargetedNoCreature",
        "Osty",
    ];

    private static readonly string[] PotionRarityOptions = ["Common", "Uncommon", "Rare", "Event", "Token"];
    private static readonly string[] PotionUsageOptions = ["CombatOnly", "AnyTime", "Automatic"];

    internal static void Initialize()
    {
        lock (InitLock)
        {
            if (_initialized)
                return;

            var toggleKeyBinding = ModSettingsBindings.WithDefault(
                ModSettingsBindings.Global<ModSettings, string>(
                    Const.ModId,
                    ModDataStore.SettingsKey,
                    settings => settings.ToggleKey,
                    (settings, value) => settings.ToggleKey = value),
                () => InputHandler.DefaultToggleBinding);

            var handScale = Refreshing(ModSettingsBindings.WithDefault(
                ModSettingsBindings.Global<ModSettings, double>(
                    Const.ModId, ModDataStore.SettingsKey,
                    s => s.HandContentScale, (s, v) => s.HandContentScale = v),
                () => 1.0d));
            var handX = Refreshing(ModSettingsBindings.WithDefault(
                ModSettingsBindings.Global<ModSettings, double>(
                    Const.ModId, ModDataStore.SettingsKey,
                    s => s.HandPositionOffsetX, (s, v) => s.HandPositionOffsetX = v),
                () => 0d));
            var handY = Refreshing(ModSettingsBindings.WithDefault(
                ModSettingsBindings.Global<ModSettings, double>(
                    Const.ModId, ModDataStore.SettingsKey,
                    s => s.HandPositionOffsetY, (s, v) => s.HandPositionOffsetY = v),
                () => 0d));
            var manualPos = Refreshing(ModSettingsBindings.WithDefault(
                ModSettingsBindings.Global<ModSettings, bool>(
                    Const.ModId, ModDataStore.SettingsKey,
                    s => s.ManualPositioningEnabled, (s, v) => s.ManualPositioningEnabled = v),
                () => false));
            var reserveWidth = Refreshing(ModSettingsBindings.WithDefault(
                ModSettingsBindings.Global<ModSettings, bool>(
                    Const.ModId, ModDataStore.SettingsKey,
                    s => s.ReserveOriginalWidth, (s, v) => s.ReserveOriginalWidth = v),
                () => true));
            var handRules = Refreshing(ModSettingsBindings.WithDefault(
                ModSettingsBindings.Global<ModSettings, List<HighlightRuleEntry>>(
                    Const.ModId, ModDataStore.SettingsKey,
                    s => s.HandHighlightRules, (s, v) => s.HandHighlightRules = v),
                () => []));

            var potionScale = Refreshing(ModSettingsBindings.WithDefault(
                ModSettingsBindings.Global<ModSettings, double>(
                    Const.ModId, ModDataStore.SettingsKey,
                    s => s.PotionContentScale, (s, v) => s.PotionContentScale = v),
                () => 1.0d));
            var potionX = Refreshing(ModSettingsBindings.WithDefault(
                ModSettingsBindings.Global<ModSettings, double>(
                    Const.ModId, ModDataStore.SettingsKey,
                    s => s.PotionPositionOffsetX, (s, v) => s.PotionPositionOffsetX = v),
                () => 0d));
            var potionY = Refreshing(ModSettingsBindings.WithDefault(
                ModSettingsBindings.Global<ModSettings, double>(
                    Const.ModId, ModDataStore.SettingsKey,
                    s => s.PotionPositionOffsetY, (s, v) => s.PotionPositionOffsetY = v),
                () => 0d));
            var potionRules = Refreshing(ModSettingsBindings.WithDefault(
                ModSettingsBindings.Global<ModSettings, List<HighlightRuleEntry>>(
                    Const.ModId, ModDataStore.SettingsKey,
                    s => s.PotionHighlightRules, (s, v) => s.PotionHighlightRules = v),
                () => []));

            RitsuLibFramework.RegisterModSettings(Const.ModId, page => page
                .WithModDisplayName(ModSettingsLocalization.T("mod.displayName", "MP Teammate View"))
                .WithTitle(ModSettingsLocalization.T("page.title", "Settings"))
                .WithDescription(ModSettingsLocalization.T("page.description",
                    "Teammate potions + hand cards: scale, offset, toggle key, and rule-based highlights."))
                .AddSection("hands", section => section
                    .WithTitle(ModSettingsLocalization.T("section.hands", "Hand Cards"))
                    .AddKeyBinding(
                        "toggle_key",
                        ModSettingsLocalization.T("toggleKey.label", "Toggle Hand Display"),
                        new ToggleKeyBinding(toggleKeyBinding),
                        true, true, true,
                        ModSettingsLocalization.T("toggleKey.description",
                            "Keyboard shortcut to show or hide teammate hand cards."))
                    .AddSlider(
                        "hand_content_scale",
                        ModSettingsLocalization.T("handContentScale.label", "Hand Card Size"),
                        handScale,
                        ModSettings.MinContentScale, ModSettings.MaxContentScale, 0.05d,
                        value => $"{value:0.00}x",
                        ModSettingsLocalization.T("handContentScale.description",
                            "Scales the mini card previews beside each teammate."))
                    .AddSlider(
                        "hand_position_offset_x",
                        ModSettingsLocalization.T("handOffsetX.label", "Hand Horizontal Offset"),
                        handX,
                        ModSettings.MinPositionOffset, ModSettings.MaxPositionOffset, 1d,
                        value => value.ToString("0"),
                        ModSettingsLocalization.T("handOffsetX.description",
                            "Moves the hand-card row horizontally."))
                    .AddSlider(
                        "hand_position_offset_y",
                        ModSettingsLocalization.T("handOffsetY.label", "Hand Vertical Offset"),
                        handY,
                        ModSettings.MinPositionOffset, ModSettings.MaxPositionOffset, 1d,
                        value => value.ToString("0"),
                        ModSettingsLocalization.T("handOffsetY.description",
                            "Moves the hand-card row vertically."))
                    .AddToggle(
                        "manual_positioning_enabled",
                        ModSettingsLocalization.T("manualPositioning.label", "Enable manual hand positioning"),
                        manualPos,
                        ModSettingsLocalization.T("manualPositioning.description",
                            "Drag each teammate hand row during combat."))
                    .AddToggle(
                        "reserve_original_width",
                        ModSettingsLocalization.T("reserveWidth.label", "Reserve original width"),
                        reserveWidth,
                        ModSettingsLocalization.T("reserveWidth.description",
                            "Keeps width reserved inside the player info row."))
                    .AddButton(
                        "reset_slot_positions",
                        ModSettingsLocalization.T("resetSlotPositions.label", "Reset dragged hand positions"),
                        ModSettingsLocalization.T("resetSlotPositions.button", "Reset"),
                        HandDisplaySettings.ClearSlotOffsets,
                        ModSettingsButtonTone.Normal,
                        ModSettingsLocalization.T("resetSlotPositions.description",
                            "Clears all saved per-slot hand-row offsets."))
                    .AddList(
                        "hand_highlight_rules",
                        ModSettingsLocalization.T("handRules.label", "Hand Highlight Rules"),
                        handRules,
                        () => new(),
                        GetCardRuleLabel,
                        GetCardRuleDescription,
                        CreateCardRuleEditor,
                        ModSettingsStructuredData.Json<HighlightRuleEntry>(),
                        ModSettingsLocalization.T("rules.add", "Add Rule"),
                        ModSettingsLocalization.T("handRules.description",
                            "Border highlights for mini cards."),
                        true, true,
                        CreateRuleHeaderAccessory))
                .AddSection("potions", section => section
                    .WithTitle(ModSettingsLocalization.T("section.potions", "Potions"))
                    .AddSlider(
                        "potion_content_scale",
                        ModSettingsLocalization.T("potionContentScale.label", "Potion Icon Size"),
                        potionScale,
                        ModSettings.MinPotionContentScale, ModSettings.MaxPotionContentScale, 0.05d,
                        value => $"{value:0.00}x",
                        ModSettingsLocalization.T("potionContentScale.description",
                            "Scales potion icons beside each teammate."))
                    .AddSlider(
                        "potion_position_offset_x",
                        ModSettingsLocalization.T("potionOffsetX.label", "Potion Horizontal Offset"),
                        potionX,
                        ModSettings.MinPositionOffset, ModSettings.MaxPositionOffset, 1d,
                        value => value.ToString("0"),
                        ModSettingsLocalization.T("potionOffsetX.description",
                            "Moves the potion row horizontally."))
                    .AddSlider(
                        "potion_position_offset_y",
                        ModSettingsLocalization.T("potionOffsetY.label", "Potion Vertical Offset"),
                        potionY,
                        ModSettings.MinPositionOffset, ModSettings.MaxPositionOffset, 1d,
                        value => value.ToString("0"),
                        ModSettingsLocalization.T("potionOffsetY.description",
                            "Moves the potion row vertically."))
                    .AddList(
                        "potion_highlight_rules",
                        ModSettingsLocalization.T("potionRules.label", "Potion Highlight Rules"),
                        potionRules,
                        () => new(),
                        GetPotionRuleLabel,
                        GetPotionRuleDescription,
                        CreatePotionRuleEditor,
                        ModSettingsStructuredData.Json<HighlightRuleEntry>(),
                        ModSettingsLocalization.T("rules.add", "Add Rule"),
                        ModSettingsLocalization.T("potionRules.description",
                            "Border highlights for potion icons."),
                        true, true,
                        CreateRuleHeaderAccessory)));

            _initialized = true;
        }
    }

    private static RefreshingBinding<T> Refreshing<T>(IModSettingsValueBinding<T> inner) => new(inner);

    private static ModSettingsText GetCardRuleLabel(HighlightRuleEntry item) =>
        ModSettingsText.Literal(item.MatchMode switch
        {
            HighlightMatchMode.Template => string.IsNullOrWhiteSpace(GetCardTemplateSummary(item))
                ? ModSettingsLocalization.Get("rules.emptyItem", "(empty rule)")
                : GetCardTemplateSummary(item),
            _ => string.IsNullOrWhiteSpace(item.Pattern)
                ? ModSettingsLocalization.Get("rules.emptyItem", "(empty rule)")
                : item.Pattern,
        });

    private static ModSettingsText GetPotionRuleLabel(HighlightRuleEntry item) =>
        ModSettingsText.Literal(item.MatchMode switch
        {
            HighlightMatchMode.Template => string.IsNullOrWhiteSpace(GetPotionTemplateSummary(item))
                ? ModSettingsLocalization.Get("rules.emptyItem", "(empty rule)")
                : GetPotionTemplateSummary(item),
            _ => string.IsNullOrWhiteSpace(item.Pattern)
                ? ModSettingsLocalization.Get("rules.emptyItem", "(empty rule)")
                : item.Pattern,
        });

    private static ModSettingsText GetCardRuleDescription(HighlightRuleEntry item)
    {
        var validation = HandDisplaySettings.ValidateRule(item);
        if (validation.IsValid)
            return ModSettingsText.Literal(
                $"{item.MatchMode} · {(item.Enabled ? ModSettingsLocalization.Get("rule.enabled", "Enabled") : ModSettingsLocalization.Get("rule.disabled", "Disabled"))} · {GetRuleColorSummary(item.ColorHex)}");
        var baseText = ModSettingsLocalization.Get(validation.LocalizationKey, "Invalid rule");
        return ModSettingsText.Literal(string.IsNullOrWhiteSpace(validation.Detail)
            ? baseText
            : $"{baseText}: {validation.Detail}");
    }

    private static ModSettingsText GetPotionRuleDescription(HighlightRuleEntry item)
    {
        var validation = PotionDisplaySettings.ValidateRule(item);
        if (validation.IsValid)
            return ModSettingsText.Literal(
                $"{item.MatchMode} · {(item.Enabled ? ModSettingsLocalization.Get("rule.enabled", "Enabled") : ModSettingsLocalization.Get("rule.disabled", "Disabled"))} · {GetRuleColorSummary(item.ColorHex)}");
        var baseText = ModSettingsLocalization.Get(validation.LocalizationKey, "Invalid rule");
        return ModSettingsText.Literal(string.IsNullOrWhiteSpace(validation.Detail)
            ? baseText
            : $"{baseText}: {validation.Detail}");
    }

    private static Control CreateCardRuleEditor(ModSettingsListItemContext<HighlightRuleEntry> itemContext)
    {
        return CreateRuleEditorCore(itemContext, isCard: true);
    }

    private static Control CreatePotionRuleEditor(ModSettingsListItemContext<HighlightRuleEntry> itemContext)
    {
        return CreateRuleEditorCore(itemContext, isCard: false);
    }

    private static Control CreateRuleEditorCore(ModSettingsListItemContext<HighlightRuleEntry> itemContext, bool isCard)
    {
        var row = new VBoxContainer();
        var modeGroup = new ButtonGroup();
        var textModeButton = CreateModeOptionButton(ModSettingsLocalization.Get("mode.text", "Text"),
            HighlightMatchMode.Text, itemContext.Item.MatchMode, modeGroup);
        var regexModeButton = CreateModeOptionButton(ModSettingsLocalization.Get("mode.regex", "Regex"),
            HighlightMatchMode.Regex, itemContext.Item.MatchMode, modeGroup);
        var templateModeButton = CreateModeOptionButton(ModSettingsLocalization.Get("mode.template", "Template"),
            HighlightMatchMode.Template, itemContext.Item.MatchMode, modeGroup);
        var modeRow =
            ModSettingsUiControlTheming.CreateSegmentedButtonRow(textModeButton, regexModeButton, templateModeButton);

        var placeholder = isCard
            ? ModSettingsLocalization.Get("rules.placeholder.card", "e.g. Exhaust / Poison / Retain")
            : ModSettingsLocalization.Get("rules.placeholder.potion", "e.g. Block / Poison / Heal");
        var patternEdit = ModSettingsUiControlTheming.CreateStyledLineEdit(itemContext.Item.Pattern, placeholder);
        var colorPicker = new ModSettingsColorControl(itemContext.Item.ColorHex, value =>
        {
            var updated = CloneRule(itemContext.Item);
            updated.ColorHex = value ?? string.Empty;
            if (RulesEqual(itemContext.Item, updated)) return;
            itemContext.Update(updated);
        });

        Control? keywordGroup = null;
        Control? typeGroup = null;
        Control? rarityGroup;
        Control? usageGroup = null;
        Control? targetGroup;
        LineEdit effectsEdit;
        ModSettingsToggleControl? upgradedToggle = null;
        ModSettingsToggleControl? playableToggle = null;
        ModSettingsToggleControl? usableToggle = null;

        if (isCard)
        {
            keywordGroup = CreateMultiSelectGroup(ModSettingsLocalization.Get("template.keywords", "Keywords"),
                CardKeywordOptions, itemContext.Item.Keywords);
            typeGroup = CreateMultiSelectGroup(ModSettingsLocalization.Get("template.types", "Types"),
                CardTypeOptions, itemContext.Item.Types);
            rarityGroup = CreateMultiSelectGroup(ModSettingsLocalization.Get("template.rarities", "Rarities"),
                CardRarityOptions, itemContext.Item.Rarities);
            targetGroup = CreateMultiSelectGroup(ModSettingsLocalization.Get("template.targets", "Target types"),
                TargetTypeOptions, itemContext.Item.TargetTypes);
            effectsEdit = ModSettingsUiControlTheming.CreateStyledLineEdit(
                string.Join(", ", itemContext.Item.EffectTerms),
                ModSettingsLocalization.Get("template.effects", "Effects (comma separated)"), height: 38f);
            upgradedToggle = CreateCompactTemplateToggle(itemContext.Item.RequireUpgraded ?? false, value =>
            {
                var updated = CloneRule(itemContext.Item);
                updated.RequireUpgraded = value ? true : null;
                if (RulesEqual(itemContext.Item, updated)) return;
                itemContext.Update(updated);
            });
            playableToggle = CreateCompactTemplateToggle(itemContext.Item.RequirePlayable ?? false, value =>
            {
                var updated = CloneRule(itemContext.Item);
                updated.RequirePlayable = value ? true : null;
                if (RulesEqual(itemContext.Item, updated)) return;
                itemContext.Update(updated);
            });
        }
        else
        {
            rarityGroup = CreateMultiSelectGroup(ModSettingsLocalization.Get("template.rarities", "Rarities"),
                PotionRarityOptions, itemContext.Item.Rarities);
            usageGroup = CreateMultiSelectGroup(ModSettingsLocalization.Get("template.usages", "Usages"),
                PotionUsageOptions, itemContext.Item.Usages);
            targetGroup = CreateMultiSelectGroup(ModSettingsLocalization.Get("template.targets", "Target types"),
                TargetTypeOptions, itemContext.Item.TargetTypes);
            effectsEdit = ModSettingsUiControlTheming.CreateStyledLineEdit(
                string.Join(", ", itemContext.Item.EffectTerms),
                ModSettingsLocalization.Get("template.effects", "Effects (comma separated)"), height: 38f);
            usableToggle = CreateCompactTemplateToggle(itemContext.Item.RequireUsable ?? false, value =>
            {
                var updated = CloneRule(itemContext.Item);
                updated.RequireUsable = value ? true : null;
                if (RulesEqual(itemContext.Item, updated)) return;
                itemContext.Update(updated);
            });
        }

        var enabledToggle = CreateRuleHeaderToggle(itemContext);
        var validationLabel = new Label
            { AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new(1f, 0.55f, 0.55f) };

        textModeButton.Toggled += pressed => { if (pressed) Save(); };
        regexModeButton.Toggled += pressed => { if (pressed) Save(); };
        templateModeButton.Toggled += pressed => { if (pressed) Save(); };
        HookTextCommit(patternEdit, Save);
        HookTextCommit(effectsEdit, Save);
        if (keywordGroup != null) HookGroup(keywordGroup, Save);
        if (typeGroup != null) HookGroup(typeGroup, Save);
        HookGroup(rarityGroup, Save);
        if (usageGroup != null) HookGroup(usageGroup, Save);
        HookGroup(targetGroup, Save);

        row.AddChild(modeRow);
        row.AddChild(patternEdit);
        if (keywordGroup != null) row.AddChild(keywordGroup);
        if (typeGroup != null) row.AddChild(typeGroup);
        row.AddChild(rarityGroup);
        if (usageGroup != null) row.AddChild(usageGroup);
        row.AddChild(targetGroup);
        row.AddChild(effectsEdit);

        if (isCard && upgradedToggle != null && playableToggle != null)
        {
            var requirementRow = ModSettingsUiControlTheming.CreateCompactToggleRow(
                ModSettingsUiControlTheming.CreateCompactToggleField(
                    ModSettingsLocalization.Get("template.upgraded", "Require upgraded"), upgradedToggle),
                ModSettingsUiControlTheming.CreateCompactToggleField(
                    ModSettingsLocalization.Get("template.playable", "Require playable"), playableToggle));
            row.AddChild(requirementRow);
        }
        else if (usableToggle != null)
        {
            var requirementRow = ModSettingsUiControlTheming.CreateCompactToggleRow(
                ModSettingsUiControlTheming.CreateCompactToggleField(
                    ModSettingsLocalization.Get("template.usable", "Require usable"), usableToggle));
            row.AddChild(requirementRow);
        }

        var colorRow = ModSettingsUiControlTheming.CreateCompactEditorRow(3,
            ModSettingsUiControlTheming.CreateCompactEditorField(
                ModSettingsLocalization.Get("rule.color", "Rule Color"), colorPicker));
        row.AddChild(colorRow);
        row.AddChild(validationLabel);
        RefreshVisibility();
        UpdateValidation();
        return row;

        HighlightMatchMode GetSelectedMode()
        {
            if (regexModeButton.ButtonPressed) return HighlightMatchMode.Regex;
            return templateModeButton.ButtonPressed ? HighlightMatchMode.Template : HighlightMatchMode.Text;
        }

        void RefreshVisibility()
        {
            var isTemplate = GetSelectedMode() == HighlightMatchMode.Template;
            patternEdit.Visible = !isTemplate;
            if (keywordGroup != null) keywordGroup.Visible = isTemplate;
            if (typeGroup != null) typeGroup.Visible = isTemplate;
            rarityGroup.Visible = isTemplate;
            if (usageGroup != null) usageGroup.Visible = isTemplate;
            targetGroup.Visible = isTemplate;
            effectsEdit.Visible = isTemplate;
            if (upgradedToggle != null) upgradedToggle.Visible = isTemplate;
            if (playableToggle != null) playableToggle.Visible = isTemplate;
            if (usableToggle != null) usableToggle.Visible = isTemplate;
        }

        void UpdateValidation()
        {
            var current = BuildUpdated();
            var validation = isCard
                ? HandDisplaySettings.ValidateRule(current)
                : PotionDisplaySettings.ValidateRule(current);
            validationLabel.Text = validation.IsValid
                ? string.Empty
                : BuildValidationMessage(validation.LocalizationKey, validation.Detail);
        }

        HighlightRuleEntry BuildUpdated() => new()
        {
            MatchMode = GetSelectedMode(),
            Pattern = patternEdit.Text.Trim(),
            ColorHex = colorPicker.ValueText.Trim(),
            Enabled = enabledToggle.ButtonPressed,
            Keywords = keywordGroup != null ? GetSelectedValues(keywordGroup) : [],
            Types = typeGroup != null ? GetSelectedValues(typeGroup) : [],
            Rarities = GetSelectedValues(rarityGroup),
            Usages = usageGroup != null ? GetSelectedValues(usageGroup) : [],
            TargetTypes = GetSelectedValues(targetGroup),
            EffectTerms = ParseCsv(effectsEdit.Text),
            RequireUpgraded = upgradedToggle is { ButtonPressed: true } ? true : null,
            RequirePlayable = playableToggle is { ButtonPressed: true } ? true : null,
            RequireUsable = usableToggle is { ButtonPressed: true } ? true : null,
        };

        void Save()
        {
            var updated = BuildUpdated();
            UpdateValidation();
            RefreshVisibility();
            if (RulesEqual(itemContext.Item, updated)) return;
            itemContext.Update(updated);
        }
    }

    private static bool RulesEqual(HighlightRuleEntry left, HighlightRuleEntry right) =>
        left.Pattern == right.Pattern
        && left.ColorHex == right.ColorHex
        && left.Enabled == right.Enabled
        && left.MatchMode == right.MatchMode
        && left.RequireUpgraded == right.RequireUpgraded
        && left.RequirePlayable == right.RequirePlayable
        && left.RequireUsable == right.RequireUsable
        && left.Keywords.SequenceEqual(right.Keywords)
        && left.Types.SequenceEqual(right.Types)
        && left.Rarities.SequenceEqual(right.Rarities)
        && left.Usages.SequenceEqual(right.Usages)
        && left.TargetTypes.SequenceEqual(right.TargetTypes)
        && left.EffectTerms.SequenceEqual(right.EffectTerms);

    private static Control CreateRuleHeaderAccessory(ModSettingsListItemContext<HighlightRuleEntry> itemContext) =>
        CreateRuleHeaderToggle(itemContext);

    private static Button CreateRuleHeaderToggle(ModSettingsListItemContext<HighlightRuleEntry> itemContext)
    {
        var button = ModSettingsUiControlTheming.CreateCompactSettingsToggleButton(
            ModSettingsLocalization.Get("rule.enabled", "Enabled"), itemContext.Item.Enabled);
        button.Toggled += pressed =>
        {
            var updated = CloneRule(itemContext.Item);
            updated.Enabled = pressed;
            if (RulesEqual(itemContext.Item, updated)) return;
            itemContext.Update(updated);
        };
        return button;
    }

    private static HighlightRuleEntry CloneRule(HighlightRuleEntry item) => new()
    {
        Pattern = item.Pattern,
        ColorHex = item.ColorHex,
        Enabled = item.Enabled,
        MatchMode = item.MatchMode,
        Keywords = [.. item.Keywords],
        Types = [.. item.Types],
        Rarities = [.. item.Rarities],
        Usages = [.. item.Usages],
        TargetTypes = [.. item.TargetTypes],
        EffectTerms = [.. item.EffectTerms],
        RequireUpgraded = item.RequireUpgraded,
        RequirePlayable = item.RequirePlayable,
        RequireUsable = item.RequireUsable,
    };

    private static ModSettingsToggleControl CreateCompactTemplateToggle(bool initialValue, Action<bool> onChanged) =>
        ModSettingsUiControlTheming.CreateCompactStateToggle(initialValue, onChanged);

    private static Button CreateModeOptionButton(string text, HighlightMatchMode mode, HighlightMatchMode selectedMode,
        ButtonGroup group) =>
        ModSettingsUiControlTheming.CreateSegmentedToggleButton(text, mode == selectedMode, group);

    private static string GetCardTemplateSummary(HighlightRuleEntry item)
    {
        var parts = new List<string>();
        if (item.Keywords.Count > 0) parts.Add($"K:{string.Join("/", item.Keywords)}");
        if (item.Types.Count > 0) parts.Add($"T:{string.Join("/", item.Types)}");
        if (item.Rarities.Count > 0) parts.Add($"R:{string.Join("/", item.Rarities)}");
        if (item.TargetTypes.Count > 0) parts.Add($"G:{string.Join("/", item.TargetTypes)}");
        if (item.EffectTerms.Count > 0) parts.Add($"E:{string.Join("/", item.EffectTerms)}");
        if (item.RequireUpgraded == true) parts.Add("Upgraded");
        if (item.RequirePlayable == true) parts.Add("Playable");
        return string.Join(" + ", parts);
    }

    private static string GetPotionTemplateSummary(HighlightRuleEntry item)
    {
        var parts = new List<string>();
        if (item.Rarities.Count > 0) parts.Add($"R:{string.Join("/", item.Rarities)}");
        if (item.Usages.Count > 0) parts.Add($"U:{string.Join("/", item.Usages)}");
        if (item.TargetTypes.Count > 0) parts.Add($"T:{string.Join("/", item.TargetTypes)}");
        if (item.EffectTerms.Count > 0) parts.Add($"E:{string.Join("/", item.EffectTerms)}");
        if (item.RequireUsable == true) parts.Add("Usable");
        return string.Join(" + ", parts);
    }

    private static string BuildValidationMessage(string key, string? detail)
    {
        var baseText = ModSettingsLocalization.Get(key, "Invalid rule");
        return string.IsNullOrWhiteSpace(detail) ? baseText : $"{baseText}: {detail}";
    }

    private static string GetRuleColorSummary(string? colorHex) =>
        string.IsNullOrWhiteSpace(colorHex)
            ? ModSettingsLocalization.Get("rule.color.default", "Default color")
            : colorHex;

    private static List<string> ParseCsv(string text) =>
        text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static Control CreateMultiSelectGroup(string labelText, IReadOnlyList<string> options,
        IReadOnlyCollection<string> selected)
    {
        var wrapper = new VBoxContainer();
        wrapper.AddThemeConstantOverride("separation", 6);
        wrapper.AddChild(new Label { Text = labelText });
        var grid = new GridContainer { Columns = 3, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 8);
        grid.AddThemeConstantOverride("v_separation", 8);
        var selectedSet = new HashSet<string>(selected, StringComparer.OrdinalIgnoreCase);
        foreach (var option in options)
            grid.AddChild(ModSettingsUiControlTheming.CreateSettingsToggleButton(option, selectedSet.Contains(option)));
        wrapper.AddChild(grid);
        return wrapper;
    }

    private static List<string> GetSelectedValues(Control group)
    {
        if (group.GetChildCount() < 2 || group.GetChild(1) is not GridContainer grid)
            return [];
        var result = new List<string>();
        foreach (var child in grid.GetChildren())
            if (child is Button { ButtonPressed: true } button)
                result.Add(button.Text);
        return result;
    }

    private static void HookGroup(Control group, Action save)
    {
        if (group.GetChildCount() < 2 || group.GetChild(1) is not GridContainer grid)
            return;
        foreach (var child in grid.GetChildren())
            if (child is Button button)
                button.Toggled += _ => save();
    }

    private static void HookTextCommit(LineEdit edit, Action save)
    {
        edit.TextSubmitted += _ =>
        {
            save();
            edit.ReleaseFocus();
        };
        edit.FocusExited += save;
    }

    private sealed class ToggleKeyBinding(IModSettingsValueBinding<string> inner)
        : IDefaultModSettingsValueBinding<string>, IStructuredModSettingsValueBinding<string>
    {
        public string ModId => inner.ModId;
        public string DataKey => inner.DataKey;
        public SaveScope Scope => inner.Scope;

        public string Read() => inner.Read();

        public void Write(string value) => inner.Write(value);

        public void Save()
        {
            inner.Save();
            MainFile.ApplyRuntimeHotkeysFromSettings();
        }

        public string CreateDefaultValue() =>
            inner is IDefaultModSettingsValueBinding<string> defaults
                ? defaults.CreateDefaultValue()
                : InputHandler.DefaultToggleBinding;

        public IStructuredModSettingsValueAdapter<string> Adapter =>
            inner is IStructuredModSettingsValueBinding<string> structured
                ? structured.Adapter
                : ModSettingsStructuredData.Json<string>();
    }

    private sealed class RefreshingBinding<T>(IModSettingsValueBinding<T> inner)
        : IDefaultModSettingsValueBinding<T>, IStructuredModSettingsValueBinding<T>
    {
        public string ModId => inner.ModId;
        public string DataKey => inner.DataKey;
        public SaveScope Scope => inner.Scope;

        public T Read() => inner.Read();

        public void Write(T value)
        {
            inner.Write(value);
            LayoutSettingsSnapshot.Invalidate();
            CardHighlightEvaluator.InvalidateRules();
            PotionDisplaySettings.Invalidate();
            TeammateViewHost.RefreshAllFromSettings();
        }

        public void Save() => inner.Save();

        public T CreateDefaultValue() =>
            inner is IDefaultModSettingsValueBinding<T> defaults ? defaults.CreateDefaultValue() : default!;

        public IStructuredModSettingsValueAdapter<T> Adapter =>
            inner is IStructuredModSettingsValueBinding<T> structured
                ? structured.Adapter
                : ModSettingsStructuredData.Json<T>();
    }
}
