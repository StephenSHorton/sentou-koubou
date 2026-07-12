using BaseLib.Abstracts;
using Godot;
using Henro.HenroCode.Extensions;

namespace Henro.HenroCode.Character;

public class HenroCardPool : CustomCardPoolModel
{
    public override string Title => Henro.CharacterId;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();

    // Soft indigo card-back tint
    public override float H => 0.64f;
    public override float S => 0.45f;
    public override float V => 0.85f;

    public override Color DeckEntryCardColor => Henro.Color;

    public override bool IsColorless => false;

    public override bool SeenByDefault => true;
}
