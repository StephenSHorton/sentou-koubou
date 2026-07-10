using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Cards.Uncommon;

/// <summary>Plot armor. Bigger hit when low HP.</summary>
public sealed class MainCharacter() : BrennenCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(14, ValueProp.Move),
        new DamageVar("LowHpDamage", 20, ValueProp.Move),
    ];

    protected override bool ShouldGlowGoldInternal => IsLowHp();

    private bool IsLowHp()
    {
        var c = Owner.Creature;
        if (c is null) return false;
        return c.CurrentHp * 2 <= c.MaxHp;
    }

    private decimal ActiveDamage =>
        IsLowHp() ? DynamicVars["LowHpDamage"].BaseValue : DynamicVars.Damage.BaseValue;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var stored = DynamicVars.Damage.BaseValue;
        DynamicVars.Damage.BaseValue = ActiveDamage;
        try
        {
            await CommonActions.CardAttack(this, play).Execute(choiceContext);
        }
        finally
        {
            DynamicVars.Damage.BaseValue = stored;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars["LowHpDamage"].UpgradeValueBy(4m);
    }
}
