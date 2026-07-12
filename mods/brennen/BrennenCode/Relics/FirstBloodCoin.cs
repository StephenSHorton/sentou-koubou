using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Relics;

/// <summary>First kill each combat grants Energy.</summary>
public sealed class FirstBloodCoin : BrennenRelic
{
    private bool _triggered;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(1)];

    public override async Task AfterRoomEntered(MegaCrit.Sts2.Core.Rooms.AbstractRoom room)
    {
        if (room is MegaCrit.Sts2.Core.Rooms.CombatRoom)
            _triggered = false;
        await Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (_triggered) return;
        if (dealer != Owner?.Creature) return;
        if (target is null || !result.WasTargetKilled) return;
        if (target.Side == Owner!.Creature!.Side) return;
        _triggered = true;
        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }
}
