using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace Brennen.BrennenCode.Relics;

/// <summary>Start combat: Frail all enemies.</summary>
public sealed class MuteCharm : BrennenRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Frail", 1)];

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom combat) return;
        if (Owner?.Creature is null) return;
        Flash();
        var ctx = new ThrowingPlayerChoiceContext();
        foreach (var enemy in combat.CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<FrailPower>(
                ctx, enemy, DynamicVars["Frail"].IntValue, Owner.Creature, null);
        }
    }
}
