using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Blake.BlakeCode.Relics;

/// <summary>
/// Starter relic. Charge starts each combat at 3 and resets to 3 after Unleash.
/// Interrupt lives on ChargePower, not this relic.
/// </summary>
public sealed class RacersGauntlet : BlakeRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("BaseCharge", Charge.DefaultBase)];

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner) return;
        if (Owner.PlayerCombatState is not { TurnNumber: 1 }) return;

        Flash();
        Charge.ResetCombatTracking(Owner);
        Charge.SetBase(Owner, Charge.DefaultBase);
        await Charge.Ensure(choiceContext, Owner);
    }
}
