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
    public override string CustomPortraitPath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();

    public override string PortraitPath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    public override string BetaPortraitPath =>
        $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
}
