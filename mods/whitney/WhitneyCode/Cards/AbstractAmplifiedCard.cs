using Godot;
using Whitney.WhitneyCode.Enchantments;
using Whitney.WhitneyCode.PatchesNModels;
using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Whitney.WhitneyCode.Cards
{
    public abstract class AbstractAmplifiedCard(
        int baseCost,
        int kickerCost,
        CardType type,
        CardRarity rarity,
        TargetType target) : AbstractWhitneyCard(baseCost, type, rarity, target)
    {
        public int KickerCost { get; } = kickerCost;

        //public bool IsAmplified { get; protected set; }
        public bool AmplifiedInPreview{
            get
            {
                if (RunState == null)
                    return false;
                if (Owner.PlayerCombatState == null) return false;
                switch (Pile)
                {
                    //if (Owner.PlayerCombatState.Hand.Cards.Contains(this))
                    case { Type: PileType.Hand } when Owner.Creature.HasPower<OneTimeOffPower>():
                        //SetAmplifyState(false, false);
                        return false;
                    case { Type: PileType.Hand } when Owner.Creature.HasPower<MillisecondPulsarsPower>() ||
                                                      Owner.Creature.HasPower<PulseMagicPower>():
                        //SetAmplifyState(true, true);
                        return true;
                    case { Type: PileType.Hand } when Owner.PlayerCombatState.Energy < EnergyCost.GetWithModifiers(CostModifiers.All):
                        //SetAmplifyState(false, false);
                        return false;
                    case { Type: PileType.Hand }:
                    {
                        if (Owner.PlayerCombatState.Energy >=
                            EnergyCost.GetWithModifiers(CostModifiers.All) + KickerCost)
                        {
                            //SetAmplifyState(true, false);
                            return true;
                        }

                        break;
                    }
                    case { Type: PileType.Discard or PileType.Draw or PileType.Exhaust }:
                        //SetAmplifyState(false, false);
                        return false;
                }

                return false;
            }
        }

        public bool AmplifiedInPlay;

        public bool PaidAmplifiedCost;
        public static readonly Color AmplifiedGlowColor = new Color("5244ff");

        //private bool _costModifiedForAmplify;

        //public bool CostModifiedForAmplify => _costModifiedForAmplify;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new EnergyVar(KickerCost)
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [
            WhitneyCardKeyWords.Amplify
        ];
        
        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            AmplifiedInPlay = PaidAmplifiedCost;
            if (cardPlay.IsAutoPlay) AmplifiedInPlay = true;
        }

        public void CalculateAmplifiedCost(ref int baseCost)
        {
            PaidAmplifiedCost = GetAmplifiedCost(ref baseCost);
        }
        private bool GetAmplifiedCost( ref int cost)
        {
            if (Owner.Creature.HasPower<OneTimeOffPower>())return false;
            if (Owner.Creature.HasPower<MillisecondPulsarsPower>() ||
                Owner.Creature.HasPower<PulseMagicPower>())
                return true;
            var costWithAmp = cost + kickerCost; 
            if ((Owner.PlayerCombatState?.Energy < costWithAmp ))return false;  
            cost = costWithAmp ;
            return true;
        }
        
        // public override IEnumerable<CardKeyword> CanonicalKeywords => [
        //     WhitneyCardKeyWords.Amplify
        // ];

        /*
                 public readonly Color AmplifiedColor = new Color(1,0,1);
        
        public void SetNCardHighlight(bool highlight)
        {
            NCard? nCard = NCombatRoom.Instance?.Ui.Hand.GetCard(this);
            if (nCard == null) Log.Info($"no ncard");
            NCardHighlight? nCardHighlight = nCard.CardHighlight;
            if (nCardHighlight == null)
            {
                Log.Info($"no NCardHighlight");
                return;
            }
            if (!CanPlay())
            {
                nCardHighlight.AnimHide();
                return;
            }
            var color = highlight ? AmplifiedColor : NCardHighlight.playableColor;
            nCardHighlight.Modulate = color;
            Log.Info($"Set Color {color}");
        }
        public virtual void ValidateAmplify()
        {
            bool oldAmplifiedInPreview = AmplifiedInPreview;
            if (RunState == null)
                return;
            if (Owner.PlayerCombatState == null) return;
            switch (Pile)
            {
                //if (Owner.PlayerCombatState.Hand.Cards.Contains(this))
                case { Type: PileType.Hand } when Owner.Creature.HasPower<OneTimeOffPower>():
                    //SetAmplifyState(false, false);
                    AmplifiedInPreview = false;
                    break;
                case { Type: PileType.Hand } when Owner.Creature.HasPower<MillisecondPulsarsPower>() ||
                                                  Owner.Creature.HasPower<PulseMagicPower>():
                    //SetAmplifyState(true, true);
                    AmplifiedInPreview = true;
                    break;
                case { Type: PileType.Hand } when Owner.PlayerCombatState.Energy < EnergyCost.GetWithModifiers(CostModifiers.All):
                    //SetAmplifyState(false, false);
                    AmplifiedInPreview = false;
                    break;
                case { Type: PileType.Hand }:
                {
                    if (Owner.PlayerCombatState.Energy >=
                        EnergyCost.GetWithModifiers(CostModifiers.All) + KickerCost)
                    {
                        //SetAmplifyState(true, false);
                        AmplifiedInPreview = true;
                    }

                    break;
                }
                case { Type: PileType.Discard or PileType.Draw or PileType.Exhaust }:
                    //SetAmplifyState(false, false);
                    AmplifiedInPreview = false;
                    break;
            }

            if (oldAmplifiedInPreview != AmplifiedInPreview)
            {
                //Log.Info($"Change ValidateAmplify:{AmplifiedInPreview}");
                SetNCardHighlight(AmplifiedInPreview);
            }
        }
        */
        // private void SetAmplifyState(bool isAmplified, bool costFree)
        // {
        //     AmplifiedInPreview = isAmplified;
        //     //IsAmplified = isAmplified;
        //     // if (isAmplified && !costFree && !_costModifiedForAmplify)
        //     // {
        //     //     EnergyCost.AddThisCombat(KickerCost);
        //     //     _costModifiedForAmplify = true;
        //     // }
        //     //
        //     // if (!isAmplified && _costModifiedForAmplify || costFree && _costModifiedForAmplify)
        //     // {
        //     //     EnergyCost.AddThisCombat(-KickerCost);
        //     //     _costModifiedForAmplify = false;
        //     // }
        // }

        /*
        public override Task AfterCardEnteredCombat(CardModel card)
        {
            if (card == this)
                ValidateAmplify();
            return Task.CompletedTask;
        }
*/
        public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
        {
            //ValidateAmplify();
            if (cardPlay.Card == this && cardPlay.IsLastInSeries) PaidAmplifiedCost = false;
            return Task.CompletedTask;
        }

        /*
        public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
        {
            if (card == this)
                ValidateAmplify();
            return Task.CompletedTask;
        }
*/
        // public override Task AfterEnergyReset(Player player)
        // {
        //     ValidateAmplify();
        //     return Task.CompletedTask;
        // }
        //
        // public override Task AfterPotionUsed(PotionModel potion, Creature? target)
        // {
        //     ValidateAmplify();
        //     return Task.CompletedTask;
        // }

        // public override Task BeforeCardAutoPlayed(CardModel card, Creature? target, AutoPlayType type)
        // {
        //     if (card == this)
        //     {
        //         SetAmplifyState(true, true);
        //     }
        //
        //     return Task.CompletedTask;
        // }
    }
}