using BaseLib.Abstracts;
using Godot;
using Whitney.WhitneyCode.Extensions;

namespace Whitney.WhitneyCode.Character;

public class WhitneyCardPool : CustomCardPoolModel
{
    public override string Title => Whitney.CharacterId;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();

    // Indigo / violet shell matching Whitney's dress + hat (clothing lock D3)
    public override float H => 0.72f;
    public override float S => 0.48f;
    public override float V => 0.78f;

    public override Color DeckEntryCardColor => Whitney.Color;

    public override bool IsColorless => false;

    /// <summary>Show the full kit in the compendium without requiring a run to "see" each card.</summary>
    public override bool SeenByDefault => true;
}
