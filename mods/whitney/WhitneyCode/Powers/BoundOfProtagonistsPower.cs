using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Whitney.WhitneyCode.Powers
{
    public class BoundOfProtagonistsPower : AbstractWhitneyPower
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        //Version 1

        // public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
        // {
        //     if (target != Owner)
        //     {
        //         return amount;
        //     }
        //     return amount * 0.5m;
        // }
        //
        // public override async Task AfterModifyingHpLostAfterOsty()
        // {
        //     await PowerCmd.Decrement(this);
        // }

        //Version 2

        // private bool _shouldTrigger;
        //
        // private bool ShouldTrigger
        // {
        //     get => _shouldTrigger;
        //     set
        //     {
        //         AssertMutable();
        //         _shouldTrigger = value;
        //     }
        // }
        //
        // public override Task BeforeTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side)
        // {
        //     if (side != Owner.Side)
        //     {
        //         return Task.CompletedTask;
        //     }
        //
        //     if (base.Owner.Block > 0)
        //     {
        //         return Task.CompletedTask;
        //     }
        //
        //     ShouldTrigger = true;
        //     return Task.CompletedTask;
        // }
        //
        // public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
        // {
        //     if (ShouldTrigger)
        //     {
        //         ShouldTrigger = false;
        //         Flash();
        //         //await CreatureCmd.GainBlock(base.Owner, base.DynamicVars.Block, null);
        //         await PowerCmd.Apply<FlightPower>(choiceContext, Owner, 1, Owner, null);
        //     }
        // }

        //Version 3

        // public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
        // {
        //     if (target != Owner)
        //     {
        //         return amount;
        //     }
        //     return amount * 0.5m;
        // }
        //
        // public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        // {
        //     if (player != Owner.Player)
        //         return;
        //     await PowerCmd.TickDownDuration(this);
        // }

        //Version 4
        // private bool _shouldTrigger;
        //
        // protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<FlightPower>()];
        //
        // public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
        // {
        //     if (side == Owner.Side)
        //     {
        //         _shouldTrigger = false;
        //     }
        //
        //     return base.BeforeSideTurnStart(choiceContext, side, participants, combatState);
        // }
        //
        // public override Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
        // {
        //     if (card.Owner == Owner.Player && card.Type is CardType.Curse or CardType.Status)
        //     {
        //         _shouldTrigger = true;
        //     }
        //
        //     return base.AfterCardExhausted(choiceContext, card, causedByEthereal);
        // }
        //
        // public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
        // {
        //     if (side != Owner.Side)
        //         return;
        //     if (_shouldTrigger)
        //     {
        //         await PowerCmd.Apply<FlightPower>(choiceContext, Owner, Amount, Owner, null);
        //     }
        //     
        //     _shouldTrigger = false;
        // }

        //Version 5

        private bool _triggerFlag;

        public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
        {
            if (side == Owner.Side)
            {
                _triggerFlag = false;
            }

            return base.BeforeSideTurnStart(choiceContext, side, participants, combatState);
        }

        public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
        {
            if (card.Type is CardType.Curse or CardType.Status && !_triggerFlag)
            {
                _triggerFlag = true;
                await PowerCmd.Apply<FlightPower>(choiceContext, Owner, Amount, Owner, null);
            }
        }
    }
}