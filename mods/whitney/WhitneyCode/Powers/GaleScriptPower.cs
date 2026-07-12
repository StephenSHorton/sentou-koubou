using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Whitney.WhitneyCode.Cards;

namespace Whitney.WhitneyCode.Powers;

/// <summary>
/// The first Amount Wind cards you play each turn draw 1.
/// Amount stacks (Gale Script upgrade / multi-apply).
/// </summary>
public sealed class GaleScriptPower : WhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Gale Script",
            "The first {Amount} [gold]Wind[/gold] card(s) you play each turn, draw 1 card.",
            "The first {Amount} [gold]Wind[/gold] card(s) you play each turn, draw 1 card.");

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
            return;
        if (cardPlay.Card is not WhitneyCard { Element: WhitneyElement.Wind })
            return;
        if (Owner.Player is null)
            return;

        // NotePlay already incremented Wind count at end of OnPlay; AfterCardPlayed is after.
        var windCount = WhitneyBrush.WindCardsThisTurn(Owner.Player);
        if (windCount > Amount)
            return;

        Flash();
        await CardPileCmd.Draw(choiceContext, 1, Owner.Player);
    }
}
