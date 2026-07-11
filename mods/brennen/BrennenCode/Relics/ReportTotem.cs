using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace Brennen.BrennenCode.Relics;

/// <summary>Open with Weak on all enemies — "After the game."</summary>
public sealed class ReportTotem : BrennenRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Weak", 1)];

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom combat) return;
        if (Owner?.Creature is null) return;
        Flash();
        var ctx = new ThrowingPlayerChoiceContext();
        foreach (var enemy in combat.CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<WeakPower>(
                ctx, enemy, DynamicVars["Weak"].IntValue, Owner.Creature, null);
        }
    }
}
