using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MpTeammateView.Data;
using MpTeammateView.Data.Models;

namespace MpTeammateView.Utils;

internal static class HandDisplaySettings
{
    private const float AutoGap = 8f;
    private const float ExtraAvoidanceShift = 12f;

    public static float GetMiniCardScale() => LayoutSettingsSnapshot.Current.MiniCardScale;

    public static Vector2 GetScaledCardSize() => LayoutSettingsSnapshot.Current.ScaledCardSize;

    public static float GetCardSpacing() => LayoutSettingsSnapshot.Current.CardSpacing;

    public static Vector2 GetUserOffset() => LayoutSettingsSnapshot.Current.UserOffset;

    public static bool IsManualPositioningEnabled() => LayoutSettingsSnapshot.Current.ManualPositioningEnabled;

    public static bool ShouldReserveOriginalWidth() => LayoutSettingsSnapshot.Current.ReserveOriginalWidth;

    public static Vector2 GetSlotOffset(int slotIndex) => LayoutSettingsSnapshot.Current.GetSlotOffset(slotIndex);

    public static void SetSlotOffset(int slotIndex, Vector2 offset)
    {
        ModDataStore.Modify<ModSettings>(ModDataStore.SettingsKey, settings =>
        {
            var entry = settings.SlotOffsets.FirstOrDefault(item => item.SlotIndex == slotIndex);
            if (entry == null)
            {
                settings.SlotOffsets.Add(new()
                {
                    SlotIndex = slotIndex,
                    OffsetX = offset.X,
                    OffsetY = offset.Y,
                });
                return;
            }

            entry.OffsetX = offset.X;
            entry.OffsetY = offset.Y;
        });
        ModDataStore.Save(ModDataStore.SettingsKey);
        LayoutSettingsSnapshot.Invalidate();
    }

    public static void ClearSlotOffsets()
    {
        ModDataStore.Modify<ModSettings>(ModDataStore.SettingsKey, settings => settings.SlotOffsets.Clear());
        ModDataStore.Save(ModDataStore.SettingsKey);
        LayoutSettingsSnapshot.Invalidate();
    }

    public static float GetContentWidth(int count) => LayoutSettingsSnapshot.Current.GetContentWidth(count);

    public static Vector2 ResolveAutoPosition(Rect2 anchorRect, Vector2 contentSize,
        Rect2 avoidRect, Rect2 viewportRect)
    {
        var preferredYOffset = LayoutSettingsSnapshot.Current.LegacyAutoOffset.Y;

        var c0 = new Vector2(anchorRect.End.X + AutoGap, anchorRect.Position.Y + preferredYOffset);
        var c1 = new Vector2(anchorRect.Position.X, anchorRect.End.Y + AutoGap);
        var c2 = new Vector2(anchorRect.Position.X, anchorRect.Position.Y - contentSize.Y - AutoGap);
        var c3 = new Vector2(anchorRect.Position.X - contentSize.X - AutoGap,
            anchorRect.Position.Y + preferredYOffset);
        var c4 = new Vector2(anchorRect.End.X + AutoGap,
            anchorRect.Position.Y + preferredYOffset + ExtraAvoidanceShift);

        var best = c0;
        var bestScore = ScoreCandidate(c0, contentSize, avoidRect, viewportRect);

        foreach (var candidate in new[] { c1, c2, c3, c4 })
        {
            var score = ScoreCandidate(candidate, contentSize, avoidRect, viewportRect);
            if (score < bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    public static bool TryGetHighlightColor(CardModel card, out Color color) =>
        CardHighlightEvaluator.TryGet(card, out color);

    public static RuleValidationResult ValidateRule(HighlightRuleEntry rule)
    {
        return rule.MatchMode switch
        {
            HighlightMatchMode.Regex => ValidateRegex(rule.Pattern),
            HighlightMatchMode.Template => ValidateCardTemplate(rule),
            _ => string.IsNullOrWhiteSpace(rule.Pattern)
                ? RuleValidationResult.Invalid("rule.validation.pattern_required")
                : RuleValidationResult.Valid(),
        };
    }

    public static Color GetRuleColor(string? colorHex)
    {
        return ColorParse.TryParseHexColor(colorHex, out var parsed)
            ? parsed
            : GetDefaultHighlightColor();
    }

    public static Color GetDefaultHighlightColor()
    {
        var settings = ModDataStore.Get<ModSettings>(ModDataStore.SettingsKey);
        return ColorParse.DefaultHighlight(settings.HighlightColorHex);
    }

    private static float ScoreCandidate(Vector2 candidate, Vector2 contentSize, Rect2 avoidRect, Rect2 viewportRect)
    {
        var candidateRect = new Rect2(candidate, contentSize);
        var overlap = candidateRect.Intersection(avoidRect);
        var penalty = overlap.Size.X * overlap.Size.Y;

        if (viewportRect.Encloses(candidateRect)) return penalty;
        var overflowLeft = Mathf.Max(0f, viewportRect.Position.X - candidateRect.Position.X);
        var overflowTop = Mathf.Max(0f, viewportRect.Position.Y - candidateRect.Position.Y);
        var overflowRight = Mathf.Max(0f, candidateRect.End.X - viewportRect.End.X);
        var overflowBottom = Mathf.Max(0f, candidateRect.End.Y - viewportRect.End.Y);
        penalty += (overflowLeft + overflowTop + overflowRight + overflowBottom) * 1000f;
        return penalty;
    }

    private static RuleValidationResult ValidateRegex(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return RuleValidationResult.Invalid("rule.validation.pattern_required");
        try
        {
            _ = Regex.Match(string.Empty, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return RuleValidationResult.Valid();
        }
        catch (ArgumentException ex)
        {
            return RuleValidationResult.Invalid("rule.validation.regex_invalid", ex.Message);
        }
    }

    private static RuleValidationResult ValidateCardTemplate(HighlightRuleEntry rule)
    {
        var hasCondition = rule.Keywords.Count > 0 || rule.Types.Count > 0 || rule.Rarities.Count > 0 ||
                           rule.TargetTypes.Count > 0 || rule.EffectTerms.Count > 0 ||
                           rule.RequireUpgraded.HasValue || rule.RequirePlayable.HasValue;
        return hasCondition
            ? RuleValidationResult.Valid()
            : RuleValidationResult.Invalid("rule.validation.template_required");
    }
}
