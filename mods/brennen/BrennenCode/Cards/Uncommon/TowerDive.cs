using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Cards.Uncommon;

/// <summary>
/// Body Slam package — deal damage equal to your current Block (does not remove Block).
/// </summary>
public sealed class TowerDive() : BrennenCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        // Preview base; actual damage is set from Block on play.
        [new DamageVar(0, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner.Creature is null || play.Target is null)
            return;

        var block = Owner.Creature.Block;
        var mult = IsUpgraded ? 2 : 1;
        var dmg = block * mult;
        if (dmg <= 0)
            return;

        await CreatureCmd.Damage(
            choiceContext,
            play.Target,
            dmg,
            ValueProp.Move,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        // Multiplier handled in OnPlay via IsUpgraded.
    }
}
