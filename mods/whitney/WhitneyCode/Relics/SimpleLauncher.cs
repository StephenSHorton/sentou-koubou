using BaseLib.Utils;
using Whitney.WhitneyCode.PatchesNModels;
using MegaCrit.Sts2.Core.Entities.Relics;


namespace Whitney.WhitneyCode.Relics;

[Pool(typeof(WhitneyRelicPool))]
public class SimpleLauncher : AbstractWhitneyRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;
}