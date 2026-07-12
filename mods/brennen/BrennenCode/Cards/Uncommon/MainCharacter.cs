using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Cards.Uncommon;

/// <summary>
/// Plot armor while healthy — tank main character, not glass carry.
/// </summary>
public sealed class MainCharacter() : BrennenCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12, ValueProp.Move),
        new BlockVar(10, ValueProp.Move),
        new BlockVar("HighHpBlock", 16, ValueProp.Move),
    ];

    protected override bool ShouldGlowGoldInternal => IsHighHp();

    private bool IsHighHp()
    {
        var c = Owner.Creature;
        if (c is null) return false;
        return c.CurrentHp * 2 > c.MaxHp;
    }

    private decimal ActiveBlock =>
        IsHighHp() ? DynamicVars["HighHpBlock"].BaseValue : DynamicVars.Block.BaseValue;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);

        var stored = DynamicVars.Block.BaseValue;
        DynamicVars.Block.BaseValue = ActiveBlock;
        try
        {
            await CommonActions.CardBlock(this, play);
        }
        finally
        {
            DynamicVars.Block.BaseValue = stored;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars["HighHpBlock"].UpgradeValueBy(4m);
    }
}
