using BaseLib.Abstracts;
using Brennen.BrennenCode.Extensions;
using Godot;

namespace Brennen.BrennenCode.Character;

public class BrennenCardPool : CustomCardPoolModel
{
    public override string Title => Brennen.CharacterId;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();

    // Warm red card-back tint
    public override float H => 0.02f;
    public override float S => 0.75f;
    public override float V => 0.9f;

    public override Color DeckEntryCardColor => Brennen.Color;

    public override bool IsColorless => false;
}
