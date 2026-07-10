using BaseLib.Abstracts;
using Brennen.BrennenCode.Extensions;
using Godot;

namespace Brennen.BrennenCode.Character;

public class BrennenRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => Brennen.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}