using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Brennen.BrennenCode.Cards.Basic;

/// <summary>
/// Signature meme card — literally feed the monster.
/// Heal target enemy to full HP. Exhaust.
/// </summary>
public sealed class Feed() : BrennenCard(1, CardType.Skill, CardRarity.Basic, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var target = play.Target;
        if (target is null)
            return;

        var missing = target.MaxHp - target.CurrentHp;
        if (missing > 0)
            await CreatureCmd.Heal(target, missing);
    }
}
