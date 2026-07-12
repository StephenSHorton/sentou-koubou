using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Cards.Rare;

/// <summary>Teamfight wipe fantasy — AOE hits, then plant a wall.</summary>
public sealed class Pentakill() : BrennenCard(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5, ValueProp.Move),
        new RepeatVar(3),
        new BlockVar(12, ValueProp.Move),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .Execute(choiceContext);
        await CommonActions.CardBlock(this, play);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(4m);
    }
}
