using BaseLib.Abstracts;
using Godot;

namespace Whitney.WhitneyCode.PatchesNModels;

public class WhitneyRelicPool : CustomRelicPoolModel
{
    // 卡池的能量图标。加载路径为“res://images/atlases/ui_atlas.sprites/card/energy_{EnergyColorName}.tres”。
    //public override string EnergyColorName => "whitney";

    public override string BigEnergyIconPath => "res://Whitney/images/charui/big_energy.png";

    public override string TextEnergyIconPath => "res://Whitney/images/charui/text_energy.png";

    public override Color LabOutlineColor => new(0f, 0.1f, 0.5f);
}
