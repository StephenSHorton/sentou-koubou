using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Whitney.WhitneyCode.Relics;

/// <summary>Turn 1 energy — pack the brushes.</summary>
public sealed class BrushCase : WhitneyRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(1)];

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner) return;
        if (Owner.PlayerCombatState is not { TurnNumber: 1 }) return;
        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }
}
