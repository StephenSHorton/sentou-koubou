using MegaCrit.Sts2.addons.mega_text;

namespace Whitney.WhitneyCode.Nodes;

public partial class MehMegaLabel : MegaLabel
{
    public override void _Ready()
    {
        MinFontSize = 32;
        MaxFontSize = 36;
        base._Ready();
    }
}
