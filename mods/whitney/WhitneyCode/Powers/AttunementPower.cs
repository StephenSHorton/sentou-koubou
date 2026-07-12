using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Whitney.WhitneyCode.Cards;

namespace Whitney.WhitneyCode.Powers;

/// <summary>
/// Elemental focus. Only buffs <b>Seals</b> (Whitney cards with SealCost &gt; 0):
/// +Amount damage and +Amount block from seal cards.
/// </summary>
public sealed class AttunementPower : WhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Attunement",
            "Your seals are charged. Seal cards deal and block additional equal to [gold]Attunement[/gold].",
            "Seals deal and block {Amount} additional.");

    private static bool IsSealSource(CardModel? cardSource) =>
        cardSource is WhitneyCard { IsSeal: true };

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner)
            return amount;
        if ((props & ValueProp.Move) == 0)
            return amount;
        if ((props & ValueProp.Unpowered) != 0)
            return amount;
        if (!IsSealSource(cardSource))
            return amount;

        return amount + Amount;
    }

    public override decimal ModifyBlockAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (target != Owner)
            return amount;
        if ((props & ValueProp.Move) == 0)
            return amount;
        if ((props & ValueProp.Unpowered) != 0)
            return amount;
        if (!IsSealSource(cardSource))
            return amount;

        return amount + Amount;
    }
}
