using BaseLib.Utils;
using Godot;
using Whitney.WhitneyCode.PatchesNModels;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Whitney.WhitneyCode.Cards;

[Pool(typeof(EventCardPool))]
public class Acceleration : AbstractAmplifiedCard //AbstractWhitneyCard
{
    public Acceleration() : base(0, 1, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
    }

    //protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Burn>()];
    //public override CardPoolModel VisualCardPool => ModelDb.CardPool<WhitneyCardPool>();

    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Concat(
    [
        new CardsVar(2),
        new DynamicVar("DrawAmp", 1)
    ]);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.OnPlay(choiceContext, cardPlay);

        var draw = Mathf.Min(Owner.PlayerCombatState!.DrawPile.Cards.Count, AmplifiedInPlay ? DynamicVars.Cards.IntValue + DynamicVars["DrawAmp"].IntValue : DynamicVars.Cards.IntValue);
        if (draw > 0)
            await CardPileCmd.Draw(choiceContext, draw, Owner);
        // if (AmplifiedInPlay)
        // {
        //     await CardPileCmd.Draw(choiceContext, DynamicVars["DrawAmp"].BaseValue, Owner);
        // }
        //await CardPileCmd.AddGeneratedCardToCombat(CombatState!.CreateCard<Burn>(Owner), PileType.Hand, addedByPlayer: true);
    }

    protected override void OnUpgrade()
    {
        //DynamicVars.Cards.UpgradeValueBy(1);
        DynamicVars["DrawAmp"].UpgradeValueBy(1);
    }
}