using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Blake.BlakeCode.Powers;

/// <summary>Whenever you play your Nth card in a turn, Rev. Amount = N (threshold).</summary>
public sealed class SlipstreamPower : BlakePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Slipstream",
            "Whenever you play your {Amount}th card in a turn, [gold]Rev[/gold].",
            "Whenever you play your {Amount}th card in a turn, [gold]Rev[/gold].");

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player) return;
        if (Owner.Player is null) return;

        // PlayIndex is 0-based → Nth card when PlayIndex + 1 == Amount
        if (cardPlay.PlayIndex + 1 != Amount) return;

        Flash();
        await Charge.Rev(choiceContext, Owner.Player, 1, cardPlay.Card);
    }
}
