using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Brennen.BrennenCode.Character;
using Brennen.BrennenCode.Extensions;

namespace Brennen.BrennenCode.Relics;

/// <summary>
/// This is the base class for your mod's relics, which is set up to load the relic's images from your mod's resources.
/// When creating a relic, right click the Relics folder and create a new file with the Custom Relic template.
/// This will generate a class that extends this one.
/// You can also just create the class manually; just make sure to inherit from this class.
///
/// The [Pool] annotation marks this relic as being tied to your specific character. Inheriting from this class means
/// that your relics don't need to invidually say which pool they should be in.
/// </summary>
[Pool(typeof(BrennenRelicPool))]
public abstract class BrennenRelic : CustomRelicModel
{
    // Id.Entry is like BRENNEN-DUO_QUEUE; asset files are duoqueue.png (no underscores).
    private string RelicStem => Id.Entry.RemovePrefix().Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();

    public override string PackedIconPath => $"{RelicStem}.png".RelicImagePath();
    protected override string PackedIconOutlinePath => $"{RelicStem}_outline.png".RelicImagePath();
    protected override string BigIconPath => $"{RelicStem}.png".BigRelicImagePath();
}