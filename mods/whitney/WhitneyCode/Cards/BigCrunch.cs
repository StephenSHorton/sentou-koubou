using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Whitney.WhitneyCode.Cards;

public class BigCrunch : AbstractWhitneyCard
{
    public BigCrunch() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Div", 5m),
        new EnergyVar(1),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("CalculatedDraw").WithMultiplier((card, _)
            => Mathf.FloorToInt(
                Mathf.Floor((PileType.Draw.GetPile(card.Owner).Cards.Count +
                             PileType.Discard.GetPile(card.Owner).Cards.Count) / 2f)
                / card.DynamicVars["Div"].IntValue))
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var draw = (int)((CalculatedVar)DynamicVars["CalculatedDraw"]).Calculate(cardPlay.Target);

        List<CardModel> list = (CardPile.GetCards(Owner, PileType.Draw, PileType.Discard)).ToList();
        await DoExhaust(choiceContext, list);

        if (draw > 0)
        {
            await CardPileCmd.Draw(choiceContext, draw, Owner);
            await PlayerCmd.GainEnergy(draw, Owner);
        }
    }

    private async Task DoExhaust(PlayerChoiceContext choiceContext, List<CardModel> cards)
    {
        int res = cards.Count / 2;
        List<CardModel> toExhaust = [];
        List<Orbital> orbitalsToExhaust = [];
        for (var i = 0; i < cards.Count; i++)
        {
            var cardModel = Owner.RunState.Rng.CombatCardSelection.NextItem(cards);
            if (cardModel != null)
            {
                if (cardModel is Orbital orbital)
                    orbitalsToExhaust.Add(orbital);
                else
                    toExhaust.Add(cardModel);
                cards.Remove(cardModel);
            }

            if (toExhaust.Count >= res) break;
        }

        toExhaust.AddRange(orbitalsToExhaust);
        if (toExhaust.Count > 0)
        {
            CardCmd.Preview(toExhaust);
            foreach (var cardModel in toExhaust)
            {
                await CardCmd.Exhaust(choiceContext, cardModel, false, cardModel.Pile?.Type != PileType.Hand);
            }

            PileType.Draw.GetPile(Owner).InvokeContentsChanged();
            PileType.Draw.GetPile(Owner).InvokeCardRemoveFinished();
            PileType.Draw.GetPile(Owner).InvokeCardAddFinished();
            PileType.Discard.GetPile(Owner).InvokeContentsChanged();
            PileType.Discard.GetPile(Owner).InvokeCardRemoveFinished();
            PileType.Discard.GetPile(Owner).InvokeCardAddFinished();
            PileType.Exhaust.GetPile(Owner).InvokeContentsChanged();
            PileType.Exhaust.GetPile(Owner).InvokeCardRemoveFinished();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Div"].UpgradeValueBy(-1);
    }
}