using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Whitney.WhitneyCode;
using Whitney.WhitneyCode.Powers;


namespace Whitney.WhitneyCode.Cards.Uncommon;

public sealed class AllaPrima() : WhitneyCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Fire;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var energy = IsBlendActive ? DynamicVars.Energy.IntValue + 1 : DynamicVars.Energy.IntValue;
        await PlayerCmd.GainEnergy(energy, Owner);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
