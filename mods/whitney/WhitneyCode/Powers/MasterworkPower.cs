using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Whitney.WhitneyCode.Powers;

/// <summary>
/// Once per turn: when you have played all 4 elements, gain 3 Ink, 2 Attunement, draw 2.
/// </summary>
public sealed class MasterworkPower : WhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Masterwork",
            "Once per turn, when you play all 4 elements in a single turn, gain [blue]3[/blue] [gold]Ink[/gold], [blue]2[/blue] [gold]Attunement[/gold], and draw [blue]2[/blue] cards.",
            "Once per turn, when you play all 4 elements, gain 3 Ink, 2 Attunement, draw 2.");

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
            return;
        if (Owner.Player is null)
            return;
        if (WhitneyBrush.MasterworkTriggeredThisTurn(Owner.Player))
            return;
        if (WhitneyBrush.DistinctElementsThisTurn(Owner.Player) < 4)
            return;

        WhitneyBrush.SetMasterworkTriggered(Owner.Player, true);
        Flash();
        await Ink.Gain(choiceContext, Owner.Player, 3, cardPlay.Card);
        await PowerCmd.Apply<AttunementPower>(choiceContext, Owner, 2, Owner, cardPlay.Card);
        await CardPileCmd.Draw(choiceContext, 2, Owner.Player);
    }
}
