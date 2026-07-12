using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using Whitney.WhitneyCode.Cards;

namespace Whitney.WhitneyCode.Powers;

/// <summary>Whenever you play a Water card, gain Amount Block.</summary>
public sealed class TideScriptPower : WhitneyPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Tide Script",
            "Whenever you play a [gold]Water[/gold] card, gain {Amount} [gold]Block[/gold].",
            "Whenever you play a [gold]Water[/gold] card, gain {Amount} [gold]Block[/gold].");

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
            return;
        if (cardPlay.Card is not WhitneyCard { Element: WhitneyElement.Water })
            return;

        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, cardPlay, false);
    }
}
