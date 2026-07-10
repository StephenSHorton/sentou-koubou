using BaseLib.Abstracts;
using Henro.HenroCode.Extensions;
using Godot;

namespace Henro.HenroCode.Character;

public class HenroPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => Henro.Color;
    

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}