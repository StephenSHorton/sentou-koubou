using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Brennen.BrennenCode.Cards.Basic;

/// <summary>
/// Signature meme — feed the monster (and yourself). Full heal both sides. Exhaust.
/// Reward-pool Uncommon (not a starter).
/// </summary>
public sealed class Feed() : BrennenCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var target = play.Target;
        if (target is not null)
        {
            var missingEnemy = target.MaxHp - target.CurrentHp;
            if (missingEnemy > 0)
                await CreatureCmd.Heal(target, missingEnemy);
        }

        if (Owner.Creature is not null)
        {
            var missingSelf = Owner.Creature.MaxHp - Owner.Creature.CurrentHp;
            if (missingSelf > 0)
                await CreatureCmd.Heal(Owner.Creature, missingSelf);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
