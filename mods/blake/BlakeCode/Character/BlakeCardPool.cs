using BaseLib.Abstracts;
using Blake.BlakeCode.Extensions;
using Godot;

namespace Blake.BlakeCode.Character;

public class BlakeCardPool : CustomCardPoolModel
{
    public override string Title => Blake.CharacterId;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();

    // Falcon blue card-back tint
    public override float H => 0.58f;
    public override float S => 0.72f;
    public override float V => 0.88f;

    public override Color DeckEntryCardColor => Blake.Color;

    public override bool IsColorless => false;

    public override bool SeenByDefault => true;
}
