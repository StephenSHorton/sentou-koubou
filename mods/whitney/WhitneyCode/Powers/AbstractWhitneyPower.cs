using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Whitney.WhitneyCode.Powers;

public abstract class AbstractWhitneyPower: CustomPowerModel
{
    public abstract override PowerType Type { get; }
    public abstract override  PowerStackType StackType { get; }
    
    public override string CustomPackedIconPath => $"res://Whitney/images/powers/{Id.Entry.ToLowerInvariant()}.png";

    public override string CustomBigIconPath => $"res://Whitney/images/powers/{Id.Entry.ToLowerInvariant()}.png";

    public override string CustomBigBetaIconPath => $"res://Whitney/images/powers/{Id.Entry.ToLowerInvariant()}.png";
}