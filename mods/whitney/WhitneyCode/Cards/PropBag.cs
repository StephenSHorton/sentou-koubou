using System.Diagnostics;
using Whitney.WhitneyCode.Powers;
using Whitney.WhitneyCode.Relics;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;

namespace Whitney.WhitneyCode.Cards;

public class PropBag : AbstractWhitneyCard
{
    public PropBag() : base(0, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    //public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.SingleplayerOnly;

    private static readonly List<RelicModel?> PoolUncommon =
    [
        ModelDb.Relic<Orichalcum>(),
        ModelDb.Relic<Vambrace>(),
        ModelDb.Relic<RippleBasin>(),
        ModelDb.Relic<GremlinHorn>(),
        ModelDb.Relic<PenNib>(),
        ModelDb.Relic<JossPaper>(),
        ModelDb.Relic<OrnamentalFan>(),
        ModelDb.Relic<LetterOpener>(),
        ModelDb.Relic<ReptileTrinket>(),
        ModelDb.Relic<Nunchaku>(),
        ModelDb.Relic<MercuryHourglass>(),
        ModelDb.Relic<Kusarigama>(),
        ModelDb.Relic<TuningFork>(),
        ModelDb.Relic<MiniatureCannon>(),
        ModelDb.Relic<ParryingShield>(),
        ModelDb.Relic<AmplifyWand>()
    ];

    private static readonly List<RelicModel?> PoolRare =
    [
        ModelDb.Relic<IceCream>(),
        ModelDb.Relic<UnceasingTop>(),
        ModelDb.Relic<RainbowRing>(),
        ModelDb.Relic<CloakClasp>(),
        ModelDb.Relic<MummifiedHand>(),
        ModelDb.Relic<IntimidatingHelmet>(),
        ModelDb.Relic<Shuriken>(),
        ModelDb.Relic<Kunai>(),
        ModelDb.Relic<GamePiece>(),
        ModelDb.Relic<SturdyClamp>(),
        ModelDb.Relic<BeatingRemnant>(),
        ModelDb.Relic<Pocketwatch>(),
        ModelDb.Relic<ArtOfWar>(),
        ModelDb.Relic<RazorTooth>(),
        ModelDb.Relic<TungstenRod>(),
        ModelDb.Relic<MagicBroom>(),
        ModelDb.Relic<ExperimentalFamiliar>()
    ];

    // public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new("relicCount", 3)];

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        //1-9: uncommon; 0: rare
        var poolUncommon = PoolUncommon.Where(r => Owner.Relics.FirstOrDefault(x => x.Id == r!.Id) == null).ToList();
        var poolRare = PoolRare.Where(r => Owner.Relics.FirstOrDefault(x => x.Id == r!.Id) == null).ToList();
        List<RelicModel?> picks = new List<RelicModel?>();
        for (var i = 0; i < DynamicVars["relicCount"].IntValue; i++)
        {
            var odd = Owner.RunState.Rng.CombatCardSelection.NextInt(10);

            RelicModel? take = null;
            while ((picks.Contains(take) || take == null) && (poolUncommon.Count > 0 || poolRare.Count > 0))
            {
                if ((odd == 0 || poolUncommon.Count == 0) && poolRare.Count > 0)
                {
                    take = poolRare.TakeRandom(1, Owner.RunState.Rng.CombatCardSelection).FirstOrDefault();
                }
                else if (poolUncommon.Count > 0)
                {
                    take = poolUncommon.TakeRandom(1, Owner.RunState.Rng.CombatCardSelection).FirstOrDefault();
                }
                else
                {
                    Log.Info("PropBag.OnPlay: no avail relic");
                    //return;
                }
            }

            picks.Add(take);
            poolUncommon.Remove(take);
            poolRare.Remove(take);
        }


        List<RelicModel?> relics = [];
        foreach (var relicModel in picks)
        {
            var relic = await Obtain(relicModel!.ToMutable(), Owner);
            relics.Add(relic);
        }


        PropBagPower? pow;
        if (Owner.Creature.HasPower<PropBagPower>())
        {
            pow = Owner.Creature.GetPower<PropBagPower>();
        }
        else
        {
            pow = await PowerCmd.Apply<PropBagPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
            pow?.ClearRelicList();
        }

        foreach (var relic in relics)
        {
            pow?.AddRelicToList(relic!);
            //Log.Info($"PropBag.OnPlay: odd: {odd},pick: {pick}, relic: {relic}, pow: {pow}");
        }
    }


    private static async Task<RelicModel> Obtain(RelicModel relic, Player player, int index = -1)
    {
        relic.AssertMutable();
        var runState = player.RunState;
        // runState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId).RelicChoices.Add(new ModelChoiceHistoryEntry(relic.Id, wasPicked: true));
        player.AddRelicInternal(relic, index);
        // if (!relic.IsStackable)
        // {
        //     player.RelicGrabBag.Remove(relic);
        //     runState.SharedRelicGrabBag.Remove(relic);
        // }
        if (LocalContext.IsMe(player))
        {
            NRun.Instance?.GlobalUi.RelicInventory.AnimateRelic(relic);
            NDebugAudioManager.Instance?.Play("relic_get.mp3");
            SaveManager.Instance.MarkRelicAsSeen(relic);
        }

        relic.FloorAddedToDeck = runState.TotalFloor;
        await relic.AfterObtained();
        return relic;
    }
}