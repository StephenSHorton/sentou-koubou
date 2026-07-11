using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Brennen.BrennenCode.Powers;

/// <summary>Secure the frontline: 5 Attacks in a turn → Plating.</summary>
public sealed class PentaSecurePower : BrennenPower
{
    private sealed class Data
    {
        public int AttacksThisTurn;
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Penta Secure",
            "Every time you play [blue]5[/blue] Attacks in a single turn, gain {Amount} [gold]Plating[/gold].",
            "Every time you play [blue]5[/blue] Attacks in a single turn, gain {Amount} [gold]Plating[/gold].");

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PlatingPower>()];

    protected override object InitInternalData() => new Data();

    public override Task AfterSideTurnStart(
        MegaCrit.Sts2.Core.Combat.CombatSide side,
        IReadOnlyList<MegaCrit.Sts2.Core.Entities.Creatures.Creature> participants,
        MegaCrit.Sts2.Core.Combat.ICombatState combatState)
    {
        if (participants.Contains(Owner))
            GetInternalData<Data>().AttacksThisTurn = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner)
            return;
        if (cardPlay.Card.Type != CardType.Attack)
            return;

        var data = GetInternalData<Data>();
        data.AttacksThisTurn++;
        if (data.AttacksThisTurn < 5)
            return;

        data.AttacksThisTurn = 0;
        Flash();
        await PowerCmd.Apply<PlatingPower>(choiceContext, Owner, Amount, Owner, null);
    }
}
