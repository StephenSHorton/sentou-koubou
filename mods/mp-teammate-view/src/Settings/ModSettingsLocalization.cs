using System.Reflection;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils;

namespace MpTeammateView.Settings;

internal static class ModSettingsLocalization
{
    private static readonly Lazy<I18N> InstanceFactory = new(() => new(
        "MpTeammateView-ModSettings",
        resourceFolders: ["MpTeammateView.Settings.Localization.ModSettings"],
        resourceAssembly: Assembly.GetExecutingAssembly()));

    public static I18N Instance => InstanceFactory.Value;

    public static ModSettingsText T(string key, string fallback) =>
        ModSettingsText.I18N(Instance, key, fallback);

    public static string Get(string key, string fallback) => Instance.Get(key, fallback);
}
