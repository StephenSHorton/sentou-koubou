using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Cards.Common;

/// <summary>Tilt-tank: trade HP, get mad Block. Still a meme.</summary>
public sealed class Tilt() : BrennenCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    private const int SelfDamage = 2;

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move),
        new BlockVar(6, ValueProp.Move),
    ];

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

        await CommonActions.CardBlock(this, play);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
