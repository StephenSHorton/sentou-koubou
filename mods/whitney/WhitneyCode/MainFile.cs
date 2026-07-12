using Godot;
using MegaCrit.Sts2.Core.Modding;

namespace Whitney.WhitneyCode;

/// <summary>
/// Whitney mod entry — kit architecture adapted from MarisaMod (Amplify / Inkbound / Saturate).
/// Mechanics: Amplify kicker costs, Inkbound enchantment (was Starlit), Saturate (was Charge-Up).
/// Theme: atelier ink witch, violet frames, Energy + brush fantasy.
/// </summary>
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Whitney";
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        // Marisa-style Entry.Init: script lookup + Harmony patches (Amplify cost UI, etc.)
        Entry.Init();
        Logger.Info("Whitney loaded — Marisa architecture, ink theme (Inkbound / Amplify / Saturate).");
    }
}
