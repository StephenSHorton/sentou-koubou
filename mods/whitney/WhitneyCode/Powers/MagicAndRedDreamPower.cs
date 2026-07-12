using Whitney.WhitneyCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Whitney.WhitneyCode.Powers;

// public class MagicAndRedDreamPower : AbstractWhitneyPower
// {
//     public override PowerType Type => PowerType.Buff;
//     public override PowerStackType StackType => PowerStackType.Counter;
//
//     private bool _toTrigger;
//
//     public override Task BeforeCardPlayed(CardPlay cardPlay)
//     {
//         if (cardPlay.Card.Owner != Owner.Player)
//             return Task.CompletedTask;
//
//         _toTrigger = false;
//         if (cardPlay.Card.Type is CardType.Attack && Owner.HasPower<ChargeUpPower>() && Owner.GetPower<ChargeUpPower>()!.CalculateMult() > 1
//             //|| cardPlay.Card is AbstractAmplifiedCard { IsAmplified: true }
//            )
//         {
//             _toTrigger = true;
//         }
//
//         return Task.CompletedTask;
//     }
//
//     public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
//     {
//         if (cardPlay.Card.Owner == Owner.Player && cardPlay.Card is AbstractAmplifiedCard { AmplifiedInPlay: true })
//         {
//             _toTrigger = true;
//         }
//
//         if (_toTrigger)
//         {
//             _toTrigger = false;
//             await CardPileCmd.Draw(context, Amount, Owner.Player!);
//
//             // var array = (await CardSelectCmd.FromHand(context, Owner.Player!, new CardSelectorPrefs(SelectionScreenPrompt, 1), null, this)).ToArray();
//             // if (array.Length != 0)
//             // {
//             //     await CardPileCmd.Add(array, PileType.Draw, CardPilePosition.Top);
//             // }
//         }
//     }
// }
public class MagicAndRedDreamPower : AbstractWhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private bool _triggeredForTurn;

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == Owner.Side)
        {
            _triggeredForTurn = false;
        }

        return base.BeforeSideTurnStart(choiceContext, side, participants, combatState);
    }

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (creator == Owner.Player && !_triggeredForTurn)
        {
            _triggeredForTurn = true;
            await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), Amount, Owner.Player!);
        }
    }
}