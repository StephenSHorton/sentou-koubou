using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Brennen.BrennenCode.Powers;

/// <summary>
/// 5th Attack each turn plays twice via ModifyCardPlayCount.
/// Amount >= 2: also gain 1 Fed when the double triggers.
/// </summary>
public sealed class PentaSecurePower : BrennenPower
{
    private bool _pendingFedOnDouble;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Penta Secure",
            "Whenever you play your 5th Attack in a single turn, that Attack triggers twice.",
            "Whenever you play your 5th Attack in a single turn, that Attack triggers twice.");

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner?.Creature != Owner)
            return playCount;
        if (card.Type != CardType.Attack)
            return playCount;
        // AttacksPlayedThisTurn counts completed plays; 4 means this is the 5th.
        if (BrennenTurnState.AttacksPlayedThisTurn != 4)
            return playCount;
        Flash();
        if (Amount >= 2)
            _pendingFedOnDouble = true;
        return playCount + 1;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!_pendingFedOnDouble)
            return;
        if (cardPlay.Card.Owner?.Creature != Owner)
            return;
        if (cardPlay.Card.Type != CardType.Attack)
            return;
        _pendingFedOnDouble = false;
        if (Owner.Player is not null)
            await Fed.Gain(choiceContext, Owner.Player, 1, cardPlay.Card);
    }
}
