using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Powers;

/// <summary>Whenever you take unblocked damage, become Tilted.</summary>
public sealed class YoureBadPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "You're Bad",
            "Whenever you take unblocked damage, become [gold]Tilted[/gold].",
            "Whenever you take unblocked damage, become [gold]Tilted[/gold].");

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0)
            return;
        if (Owner.Player is null)
            return;

        Flash();
        await Tilted.Become(choiceContext, Owner.Player, cardSource);
    }
}
