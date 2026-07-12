using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Powers;

/// <summary>Tank snowball: on kill → Block. Don't throw the lead.</summary>
public sealed class SnowballPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Snowball",
            "Whenever you kill an enemy, gain {Amount} [gold]Block[/gold].",
            "Whenever you kill an enemy, gain {Amount} [gold]Block[/gold].");

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner)
            return;
        if (target is null)
            return;
        if (!result.WasTargetKilled)
            return;
        if (target.Side == Owner.Side)
            return;

        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}
