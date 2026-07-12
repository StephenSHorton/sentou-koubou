using BaseLib.Abstracts;

namespace Whitney.WhitneyCode.PatchesNModels;

public class WhitneyPotionPool : CustomPotionPoolModel
{
    // 卡池的能量图标。加载路径为“res://images/atlases/ui_atlas.sprites/card/energy_{EnergyColorName}.tres”。
    public override string EnergyColorName => "whitney";
}
