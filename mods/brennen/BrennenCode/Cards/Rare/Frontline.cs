using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Brennen.BrennenCode.Cards.Rare;

/// <summary>
/// Multiplayer "I'm the tank" — vanilla TankPower:
/// you take 2× powered attacks; allies get GuardedPower (0.5×).
/// MultiplayerOnly (see reference/sts2/MULTIPLAYER_CARDS.md).
/// </summary>
public sealed class Frontline() : BrennenCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner.Creature is null)
            return;
        await CreatureCmd.TriggerAnim(
            Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<TankPower>(
            choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
