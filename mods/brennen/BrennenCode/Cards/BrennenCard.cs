using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Brennen.BrennenCode.Character;
using Brennen.BrennenCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Brennen.BrennenCode.Cards;

[Pool(typeof(BrennenCardPool))]
public abstract class BrennenCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    // Id.Entry e.g. BRENNEN-MAIN_CHARACTER → maincharacter.png (files have no underscores).
    private string PortraitStem =>
        Id.Entry.RemovePrefix().Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();

    public override string CustomPortraitPath => $"{PortraitStem}.png".BigCardImagePath();
    public override string PortraitPath => $"{PortraitStem}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{PortraitStem}.png".CardImagePath();
}
