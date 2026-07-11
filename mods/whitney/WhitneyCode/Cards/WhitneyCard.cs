using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Whitney.WhitneyCode.Character;
using Whitney.WhitneyCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Whitney.WhitneyCode.Cards;

[Pool(typeof(WhitneyCardPool))]
public abstract class WhitneyCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    /// <summary>Ink required to play. 0 = free. Checked via <see cref="IsPlayable"/>.</summary>
    protected virtual int InkCost => 0;

    protected override bool IsPlayable =>
        Ink.CanAfford(Owner, InkCost);

    /// <summary>Gold glow when the seal is ready (Ink paid).</summary>
    protected override bool ShouldGlowGoldInternal =>
        InkCost > 0 && Ink.CanAfford(Owner, InkCost);

    public override string CustomPortraitPath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();

    public override string PortraitPath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    public override string BetaPortraitPath =>
        $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
}
