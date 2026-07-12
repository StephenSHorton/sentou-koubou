using System.Threading.Tasks;
using Brennen.BrennenCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Brennen.BrennenCode.Cards.Uncommon;

/// <summary>
/// Solo "I'm the tank" — double damage taken, SotT Block.
/// Multiplayer half-ally package is Frontline (TankPower).
/// </summary>
public sealed class MainTank() : BrennenCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<MainTankPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("MainTank", 6)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner.Creature is null)
            return;

        await CreatureCmd.TriggerAnim(
            Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<MainTankPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["MainTank"].IntValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MainTank"].UpgradeValueBy(3m);
    }
}
