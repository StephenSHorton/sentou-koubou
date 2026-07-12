using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Blake.BlakeCode.Relics;

/// <summary>Uncommon. First Rev each turn → Block.</summary>
public sealed class PitCrew : BlakeRelic
{
    private int _lastRevTurn = -1;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(3, ValueProp.Unpowered)];

    /// <summary>Called from Charge.Rev when this relic is present.</summary>
    public async Task OnRev(PlayerChoiceContext choiceContext)
    {
        if (Owner.Creature is null) return;
        var turn = Owner.PlayerCombatState?.TurnNumber ?? -1;
        if (turn == _lastRevTurn) return;
        _lastRevTurn = turn;

        Flash();
        await CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars.Block.BaseValue,
            ValueProp.Unpowered,
            null);
    }
}
