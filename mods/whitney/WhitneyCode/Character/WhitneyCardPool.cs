using BaseLib.Abstracts;
using Godot;
using Whitney.WhitneyCode.Extensions;

namespace Whitney.WhitneyCode.Character;

public class WhitneyCardPool : CustomCardPoolModel
{
    public override string Title => Whitney.CharacterId;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();

    // Soft sage card-back tint (atelier parchment)
    public override float H => 0.38f;
    public override float S => 0.28f;
    public override float V => 0.82f;

    public override Color DeckEntryCardColor => Whitney.Color;

    public override bool IsColorless => false;
}
