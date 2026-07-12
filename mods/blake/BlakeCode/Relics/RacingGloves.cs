using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Blake.BlakeCode.Relics;

/// <summary>Common. Base Charge +2.</summary>
public sealed class RacingGloves : BlakeRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("BaseBonus", 2)];

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner) return;
        if (Owner.PlayerCombatState is not { TurnNumber: 1 }) return;

        Flash();
        Charge.AddBase(Owner, DynamicVars["BaseBonus"].IntValue);
        await Charge.Ensure(choiceContext, Owner);
        // Bump current Charge to new base if still at old floor.
        var power = Owner.Creature?.GetPower<Powers.ChargePower>();
        if (power is not null && power.Amount < Charge.GetBase(Owner))
        {
            var delta = Charge.GetBase(Owner) - power.Amount;
            await MegaCrit.Sts2.Core.Commands.PowerCmd.ModifyAmount(
                choiceContext, power, delta, Owner.Creature, null);
        }
    }
}
