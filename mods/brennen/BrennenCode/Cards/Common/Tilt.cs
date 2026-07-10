using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Cards.Common;

/// <summary>Emotional damage — both ways.</summary>
public sealed class Tilt() : BrennenCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    private const int SelfDamage = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(9, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);

        if (Owner.Creature is not null)
        {
            await CreatureCmd.Damage(
                choiceContext,
                [Owner.Creature],
                SelfDamage,
                ValueProp.Unblockable | ValueProp.Unpowered,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
