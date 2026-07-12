using BaseLib.Abstracts;
using BaseLib.Extensions;
using Blake.BlakeCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Blake.BlakeCode.Powers;

public abstract class BlakePower : CustomPowerModel
{
    private string PowerStem =>
        Id.Entry.RemovePrefix().Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();

    public override string CustomPackedIconPath => $"{PowerStem}.png".PowerImagePath();
    public override string CustomBigIconPath => $"{PowerStem}.png".BigPowerImagePath();

    public abstract override PowerType Type { get; }
    public abstract override PowerStackType StackType { get; }
}
