using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MpTeammateView.Data;
using MpTeammateView.Data.Models;

namespace MpTeammateView.Utils;

/// <summary>Caches compiled hand-card highlight rules for the hot path.</summary>
internal static class CardHighlightEvaluator
{
    private static readonly List<string> CandidateBuffer = [];
    private static CompiledRule[] _compiledRules = [];
    private static int _rulesVersion;
    private static int _compiledForVersion = -1;

    public static void InvalidateRules() => _rulesVersion++;

    public static bool TryGet(CardModel card, out Color color)
    {
        EnsureCompiled();
        return Evaluate(card, out color);
    }

    private static void EnsureCompiled()
    {
        if (_compiledForVersion == _rulesVersion) return;

        var rules = ModDataStore.Get<ModSettings>(ModDataStore.SettingsKey).HandHighlightRules;
        var compiled = new List<CompiledRule>(rules.Count);

        foreach (var entry in from rule in rules where rule.Enabled select CompileRule(rule))
        {
            if (entry.Source == null) continue;
            compiled.Add(entry);
        }

        _compiledRules = compiled.ToArray();
        _compiledForVersion = _rulesVersion;
    }

    private static bool Evaluate(CardModel card, out Color color)
    {
        if (_compiledRules.Length == 0)
        {
            color = default;
            return false;
        }

        string[]? candidates = null;
        foreach (var rule in _compiledRules)
        {
            if (!MatchesTemplateFilters(rule.Source!, card)) continue;

            if (!rule.NeedsTextCandidates)
            {
                color = rule.Color;
                return true;
            }

            candidates ??= BuildCandidates(card);
            if (!MatchesText(rule, candidates)) continue;
            color = rule.Color;
            return true;
        }

        color = default;
        return false;
    }

    private static bool MatchesText(CompiledRule rule, string[] candidates)
    {
        return rule.Source!.MatchMode switch
        {
            HighlightMatchMode.Regex => candidates.Any(rule.Regex!.IsMatch),
            HighlightMatchMode.Template => MatchesEffectTerms(rule.EffectTermsNormalized!, candidates),
            _ => candidates.Any(text =>
                text.Contains(rule.PatternNormalized!, StringComparison.OrdinalIgnoreCase)),
        };
    }

    private static bool MatchesEffectTerms(string[] normalizedTerms, string[] candidates) =>
        normalizedTerms.All(term =>
            candidates.Any(text => text.Contains(term, StringComparison.OrdinalIgnoreCase)));

    private static bool MatchesTemplateFilters(HighlightRuleEntry rule, CardModel card)
    {
        if (rule.MatchMode != HighlightMatchMode.Template) return true;

        if (rule.Keywords.Count > 0 && !rule.Keywords.All(required =>
                card.CanonicalKeywords.Any(keyword =>
                    string.Equals(keyword.ToString(), required, StringComparison.OrdinalIgnoreCase))))
            return false;
        if (rule.Types.Count > 0 && !rule.Types.Any(type =>
                string.Equals(type, card.Type.ToString(), StringComparison.OrdinalIgnoreCase)))
            return false;
        if (rule.Rarities.Count > 0 && !rule.Rarities.Any(rarity =>
                string.Equals(rarity, card.Rarity.ToString(), StringComparison.OrdinalIgnoreCase)))
            return false;
        if (rule.TargetTypes.Count > 0 && !rule.TargetTypes.Any(target =>
                string.Equals(target, card.TargetType.ToString(), StringComparison.OrdinalIgnoreCase)))
            return false;
        if (rule.RequireUpgraded.HasValue && card.IsUpgraded != rule.RequireUpgraded.Value)
            return false;
        if (rule.RequirePlayable.HasValue && card.CanPlay() != rule.RequirePlayable.Value)
            return false;
        return true;
    }

    private static CompiledRule CompileRule(HighlightRuleEntry rule)
    {
        var color = HandDisplaySettings.GetRuleColor(rule.ColorHex);

        switch (rule.MatchMode)
        {
            case HighlightMatchMode.Regex:
                if (string.IsNullOrWhiteSpace(rule.Pattern))
                    return default;
                try
                {
                    var regex = new Regex(rule.Pattern,
                        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    return new(rule, color, true, regex, null, null);
                }
                catch (ArgumentException)
                {
                    return default;
                }

            case HighlightMatchMode.Template:
                if (!TemplateHasAnyCondition(rule))
                    return default;
                if (rule.EffectTerms.Count == 0)
                    return new(rule, color, false, null, null, null);
                var terms = rule.EffectTerms.Select(NormalizeForMatch).ToArray();
                return new(rule, color, true, null, null, terms);

            default:
                if (string.IsNullOrWhiteSpace(rule.Pattern))
                    return default;
                return new(rule, color, true, null, NormalizeForMatch(rule.Pattern), null);
        }
    }

    private static bool TemplateHasAnyCondition(HighlightRuleEntry rule) =>
        rule.Keywords.Count > 0
        || rule.Types.Count > 0
        || rule.Rarities.Count > 0
        || rule.TargetTypes.Count > 0
        || rule.EffectTerms.Count > 0
        || rule.RequireUpgraded.HasValue
        || rule.RequirePlayable.HasValue;

    private static string[] BuildCandidates(CardModel card)
    {
        CandidateBuffer.Clear();
        foreach (var keyword in card.CanonicalKeywords)
            AppendCandidate(keyword.ToString());
        foreach (var hoverTip in card.HoverTips)
        {
            if (hoverTip is not HoverTip concrete) continue;
            AppendCandidate(concrete.Title);
            AppendCandidate(concrete.Description);
        }

        var result = CandidateBuffer.ToArray();
        CandidateBuffer.Clear();
        return result;
    }

    private static void AppendCandidate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;
        var normalized = NormalizeForMatch(raw);
        if (string.IsNullOrWhiteSpace(normalized)) return;
        CandidateBuffer.Add(normalized);
    }

    private static string NormalizeForMatch(string text)
    {
        var withoutBbCode = text.StripBbCode();
        var withoutHtml = NSearchBar.RemoveHtmlTags(withoutBbCode);
        return NSearchBar.Normalize(withoutHtml);
    }

    private readonly record struct CompiledRule(
        HighlightRuleEntry? Source,
        Color Color,
        bool NeedsTextCandidates,
        Regex? Regex,
        string? PatternNormalized,
        string[]? EffectTermsNormalized);
}
