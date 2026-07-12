using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Whitney.WhitneyCode.Cards;

namespace Whitney.WhitneyCode.Powers;

/// <summary>Whenever you play an Earth card, gain Amount Attunement.</summary>
public sealed class StoneScriptPower : WhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Stone Script",
            "Whenever you play an [gold]Earth[/gold] card, gain {Amount} [gold]Attunement[/gold].",
            "Whenever you play an [gold]Earth[/gold] card, gain {Amount} [gold]Attunement[/gold].");

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
            return;
        if (cardPlay.Card is not WhitneyCard { Element: WhitneyElement.Earth })
            return;

        Flash();
        await PowerCmd.Apply<AttunementPower>(
            choiceContext, Owner, Amount, Owner, cardPlay.Card);
    }
}
