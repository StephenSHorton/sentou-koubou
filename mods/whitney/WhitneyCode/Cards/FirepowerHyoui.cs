using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Whitney.WhitneyCode.Cards;

public class FirepowerHyoui : AbstractWhitneyCard
{
    public FirepowerHyoui() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    {
    }

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Power", 8)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ChargeUpPower>()
    ];

    //public override string PortraitPath => "res://Whitney/images/cards/whitney-test_whitney_card.png";

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsUpgraded)
        {
            var enumerable = CombatState!.GetTeammatesOf(Owner.Creature).Where(c => c is { IsAlive: true, IsPlayer: true } && c != Owner.Creature);
            foreach (var creature in enumerable)
            {
                await PowerCmd.Apply<ChargeUpPower>(choiceContext, creature, DynamicVars["Power"].BaseValue, Owner.Creature, this);
            }
        }
        else
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await PowerCmd.Apply<ChargeUpPower>(choiceContext, cardPlay.Target, DynamicVars["Power"].BaseValue, Owner.Creature, this);
        }
    }
}