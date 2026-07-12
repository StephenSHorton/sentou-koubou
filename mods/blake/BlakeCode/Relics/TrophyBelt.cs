using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rooms;

namespace Blake.BlakeCode.Relics;

/// <summary>
/// Rare. Charge persists between combats (still resets on Unleash).
/// Persistence is consumed in Charge.Ensure via PersistedCharge.
/// </summary>
public sealed class TrophyBelt : BlakeRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner.Creature is null) return Task.CompletedTask;
        Charge.OnCombatEnd(Owner, persist: true);
        Flash();
        return Task.CompletedTask;
    }
}
