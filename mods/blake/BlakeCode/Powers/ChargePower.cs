using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Blake.BlakeCode.Powers;

/// <summary>
/// Blake's fist meter. Amount is the stored Charge value.
/// Interrupt: unblocked enemy attack damage halves Charge (floor = base).
/// </summary>
public sealed class ChargePower : BlakePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Charge",
            "Your fist is winding up. [gold]Unleash[/gold] spends this for damage, then resets to base. Taking unblocked attack damage [gold]halves[/gold] Charge.",
            "You have {Amount} [gold]Charge[/gold]. Unblocked attack damage halves it.");

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (result.UnblockedDamage <= 0) return;
        // Clean hits from enemies only (not self-damage / thorns edge cases from allies).
        if (dealer is null || !dealer.IsEnemy) return;

        Flash();
        await Charge.Interrupt(choiceContext, Owner, cardSource);
    }
}
