using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Cards.Common;

/// <summary>HP → Energy package — pay life for blue buff.</summary>
public sealed class Buyback() : BrennenCard(0, CardType.Skill, CardRarity.Common, TargetType.None)
{
    private const int SelfDamage = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
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

        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}
