using BaseLib.Abstracts;
using BaseLib.Utils;
using Whitney.WhitneyCode.PatchesNModels;

namespace Whitney.WhitneyCode.Relics;

[Pool(typeof(WhitneyRelicPool))]
public abstract class AbstractWhitneyRelic : CustomRelicModel
{
    // 小图标
    public override string PackedIconPath => $"res://Whitney/images/relics/{Id.Entry.ToLowerInvariant()}.png";
    // 轮廓图标
    protected override string PackedIconOutlinePath => $"res://Whitney/images/relics/{Id.Entry.ToLowerInvariant()}.png";
    // 大图标
    protected override string BigIconPath => $"res://Whitney/images/relics/{Id.Entry.ToLowerInvariant()}.png";
}