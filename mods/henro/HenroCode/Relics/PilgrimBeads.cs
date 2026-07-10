using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rooms;

namespace Henro.HenroCode.Relics;

/// <summary>
/// Starting relic. Heal a little after each combat victory — the pilgrim's rest.
/// </summary>
public sealed class PilgrimBeads : HenroRelic
{
    public const int HealAmount = 4;

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (Owner.Creature is null)
            return;

        Flash();
        await CreatureCmd.Heal(Owner.Creature, HealAmount);
    }
}
