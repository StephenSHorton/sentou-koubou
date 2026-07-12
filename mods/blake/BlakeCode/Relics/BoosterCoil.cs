using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Blake.BlakeCode.Relics;

/// <summary>Boss. At the start of each combat, Rev twice.</summary>
public sealed class BoosterCoil : BlakeRelic
{
    // STS2 has no separate Boss rarity; treated as high-end Rare for pool purposes.
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner) return;
        if (Owner.PlayerCombatState is not { TurnNumber: 1 }) return;

        Flash();
        await Charge.Ensure(choiceContext, Owner);
        await Charge.Rev(choiceContext, Owner, times: 2);
    }
}
