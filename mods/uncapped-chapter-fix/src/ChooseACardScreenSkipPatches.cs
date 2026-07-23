using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace UncappedChapterFix;

/// <summary>
/// Vanilla remote path in <see cref="CardSelectCmd.FromChooseACardScreen"/>:
/// <c>result = (num &lt; 0) ? null : cards[num]</c>.
/// Skip is usually encoded as -1 (IndexOf null), but peers sometimes send the
/// card-reward-style sentinel <c>indexes == cards.Count</c> (e.g. 3 on a 3-card
/// Hefty Tablet offer). That throws ArgumentOutOfRangeException on the host after
/// the choice ID was reserved → nextChoiceId drift → Neow-exit StateDivergence.
///
/// Replaces the method with the same flow plus bounds-checked skip handling.
/// Also allows &gt;3 cards (Downfall NOPs the vanilla throw for that case).
/// </summary>
public static class ChooseACardScreenSkipPatches
{
    public static bool TryApply(Harmony harmony)
    {
        MethodInfo? target = AccessTools.Method(
            typeof(CardSelectCmd),
            nameof(CardSelectCmd.FromChooseACardScreen));

        if (target == null)
        {
            MainFile.Logger.Warn(
                "CardSelectCmd.FromChooseACardScreen not found — skip OOB harden.");
            return false;
        }

        harmony.Patch(
            target,
            prefix: new HarmonyMethod(typeof(ChooseACardScreenSkipPatches), nameof(Prefix)));

        MainFile.Logger.Info(
            "Patched CardSelectCmd.FromChooseACardScreen prefix (OOB index→null skip).");
        return true;
    }

    /// <summary>
    /// Full replacement so remote skip never indexes out of range.
    /// </summary>
    public static bool Prefix(
        PlayerChoiceContext context,
        IReadOnlyList<CardModel> cards,
        Player player,
        bool canSkip,
        ref Task<CardModel?> __result)
    {
        __result = FromChooseACardScreenSafe(context, cards, player, canSkip);
        return false; // skip original (and Downfall's MoveNext transpiler on it)
    }

    private static async Task<CardModel?> FromChooseACardScreenSafe(
        PlayerChoiceContext context,
        IReadOnlyList<CardModel> cards,
        Player player,
        bool canSkip)
    {
        // Vanilla throws when cards.Count > 3; Downfall NOPs that. Keep allowing >3.
        if (cards.Count == 0)
        {
            CardSelectCmd.ReportSoftlock();
            return null;
        }

        CardModel? result;
        if (CardSelectCmd.Selector != null)
        {
            result = (await CardSelectCmd.Selector.GetSelectedCards(cards, 0, 1)).FirstOrDefault();
        }
        else
        {
            uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
            await context.SignalPlayerChoiceBegun(PlayerChoiceOptions.None);

            if (CardSelectCmd.ShouldSelectLocalCard(player))
            {
                NPlayerHand.Instance?.CancelAllCardPlay();
                NChooseACardSelectionScreen? screen =
                    NChooseACardSelectionScreen.ShowScreen(cards, canSkip);

                if (screen == null)
                {
                    // Same as no selection / soft-fail — encode skip for remote peers.
                    RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                        player, choiceId, PlayerChoiceResult.FromIndex(-1));
                    result = null;
                }
                else
                {
                    if (LocalContext.IsMe(player))
                    {
                        foreach (CardModel card in cards)
                            SaveManager.Instance.MarkCardAsSeen(card);
                    }

                    result = (await screen.CardsSelected()).FirstOrDefault();
                    // Explicit skip: null or not-in-list → -1 (never cards.Count OOB sentinel).
                    int index = result == null ? -1 : IndexOfCard(cards, result);
                    if (index < 0)
                        index = -1;

                    RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                        player, choiceId, PlayerChoiceResult.FromIndex(index));
                }
            }
            else
            {
                int num = (await RunManager.Instance.PlayerChoiceSynchronizer
                    .WaitForRemoteChoice(player, choiceId)).AsIndex();

                // Treat OOB / reward-style skip sentinel as null (canSkip).
                if (num < 0 || num >= cards.Count)
                {
                    if (num >= cards.Count)
                    {
                        MainFile.Logger.Warn(
                            $"FromChooseACardScreen remote index {num} out of range " +
                            $"(cards={cards.Count}, canSkip={canSkip}) — treating as skip " +
                            "(prevents host choice-ID drift / ArgumentOutOfRangeException).");
                    }

                    result = null;
                }
                else
                {
                    result = cards[num];
                }
            }

            await context.SignalPlayerChoiceEnded();
        }

        CardSelectCmd.LogChoice(player, new CardModel?[] { result });
        return result;
    }

    private static int IndexOfCard(IReadOnlyList<CardModel> cards, CardModel card)
    {
        // Match vanilla list IndexOf (reference identity for offer instances).
        for (int i = 0; i < cards.Count; i++)
        {
            if (ReferenceEquals(cards[i], card))
                return i;
        }

        return -1;
    }
}
