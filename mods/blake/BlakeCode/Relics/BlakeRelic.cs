using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Blake.BlakeCode.Character;
using Blake.BlakeCode.Extensions;

namespace Blake.BlakeCode.Relics;

[Pool(typeof(BlakeRelicPool))]
public abstract class BlakeRelic : CustomRelicModel
{
    private string RelicStem =>
        Id.Entry.RemovePrefix().Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();

    public override string PackedIconPath => $"{RelicStem}.png".RelicImagePath();
    protected override string PackedIconOutlinePath => $"{RelicStem}_outline.png".RelicImagePath();
    protected override string BigIconPath => $"{RelicStem}.png".BigRelicImagePath();
}
