using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Blake.BlakeCode.Powers;

/// <summary>
/// One-turn tracker for Perfect Shield: if you take no unblocked attack damage, Rev at end of turn.
/// </summary>
public sealed class PerfectShieldTrackerPower : BlakePower
{
    private bool _tookCleanHit;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Perfect Shield",
            "If you block ALL attack damage this turn, [gold]Rev[/gold] at end of turn.",
            "If you block ALL attack damage this turn, [gold]Rev[/gold] at end of turn.");

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (dealer is null || !dealer.IsEnemy) return;
        if (result.UnblockedDamage > 0)
            _tookCleanHit = true;
        await Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        if (Owner.Player is null) return;

        if (!_tookCleanHit)
        {
            Flash();
            await Charge.Rev(choiceContext, Owner.Player, 1);
        }

        // Self-remove via amount zero / single-turn — Amount stays; we remove explicitly.
        await MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(this);
    }
}
