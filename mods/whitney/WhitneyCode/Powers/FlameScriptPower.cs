using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Whitney.WhitneyCode.Cards;

namespace Whitney.WhitneyCode.Powers;

/// <summary>Whenever you play a Fire card, deal Amount damage to a random enemy.</summary>
public sealed class FlameScriptPower : WhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Flame Script",
            "Whenever you play a [gold]Fire[/gold] card, deal {Amount} damage to a random enemy.",
            "Whenever you play a [gold]Fire[/gold] card, deal {Amount} damage to a random enemy.");

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
            return;
        if (cardPlay.Card is not WhitneyCard { Element: WhitneyElement.Fire })
            return;
        if (Owner.Player is null || Owner.CombatState is null)
            return;

        var enemies = Owner.CombatState.HittableEnemies.ToList();
        if (enemies.Count == 0)
            return;

        Flash();
        var target = enemies[System.Random.Shared.Next(enemies.Count)];
        await CreatureCmd.Damage(
            choiceContext,
            target,
            Amount,
            ValueProp.Unpowered,
            Owner,
            cardPlay.Card);
    }
}
