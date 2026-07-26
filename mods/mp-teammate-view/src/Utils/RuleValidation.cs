namespace MpTeammateView.Utils;

internal readonly record struct RuleValidationResult(bool IsValid, string LocalizationKey, string? Detail)
{
    public static RuleValidationResult Valid() => new(true, string.Empty, null);

    public static RuleValidationResult Invalid(string localizationKey, string? detail = null) =>
        new(false, localizationKey, detail);
}
