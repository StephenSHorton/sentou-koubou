using BaseLib.Abstracts;
using Blake.BlakeCode.Extensions;
using Godot;

namespace Blake.BlakeCode.Character;

public class BlakeRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => Blake.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}
