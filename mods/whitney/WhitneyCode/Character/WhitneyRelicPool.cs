using BaseLib.Abstracts;
using Whitney.WhitneyCode.Extensions;
using Godot;

namespace Whitney.WhitneyCode.Character;

public class WhitneyRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => Whitney.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}