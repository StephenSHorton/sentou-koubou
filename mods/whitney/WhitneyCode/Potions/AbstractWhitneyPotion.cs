using BaseLib.Abstracts;
using BaseLib.Utils;
using Whitney.WhitneyCode.PatchesNModels;

namespace Whitney.WhitneyCode.Potions;

[Pool(typeof(WhitneyPotionPool))]
public abstract class AbstractWhitneyPotion : CustomPotionModel
{
    public override string? CustomPackedImagePath => $"res://Whitney/images/potions/{Id.Entry.ToLowerInvariant()}.png"; //GetImagePath();
    public override string? CustomPackedOutlinePath => $"res://Whitney/images/potions/{Id.Entry.ToLowerInvariant()}_outline.png"; //GetOutlinePath();

    // protected abstract string GetImagePath();
    // protected abstract string GetOutlinePath();

    protected const string GodotIconPath = "res://icon.svg";

    // protected string PotionIconPath => $"res://Whitney/images/potions/{Id.Entry.ToLowerInvariant()}.png";
    // protected string PotionOutlinePath => $"res://Whitney/images/potions/{Id.Entry.ToLowerInvariant()}_outline.png";
}