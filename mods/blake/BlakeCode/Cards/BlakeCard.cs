using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Blake.BlakeCode.Character;
using Blake.BlakeCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Blake.BlakeCode.Cards;

[Pool(typeof(BlakeCardPool))]
public abstract class BlakeCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    private string PortraitStem =>
        Id.Entry.RemovePrefix().Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();

    public override string CustomPortraitPath => $"{PortraitStem}.png".BigCardImagePath();
    public override string PortraitPath => $"{PortraitStem}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{PortraitStem}.png".CardImagePath();
}
