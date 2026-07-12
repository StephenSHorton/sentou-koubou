using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Powers;

/// <summary>Main character tank: every Skill → Block. It's still about you.</summary>
public sealed class MainCharacterPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Main Character Syndrome",
            "Whenever you play a Skill, gain {Amount} [gold]Block[/gold].",
            "Whenever you play a Skill, gain {Amount} [gold]Block[/gold].");

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner)
            return;
        if (cardPlay.Card.Type != CardType.Skill)
            return;

        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}
