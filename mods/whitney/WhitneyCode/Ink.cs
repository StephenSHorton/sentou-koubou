using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Whitney.WhitneyCode;

/// <summary>
/// Whitney's second mana — stored as vanilla <b>Stars</b> so the star counter
/// appears next to Energy (Regent placement). Card/loc copy still says "Ink".
/// Hard cap: <see cref="MaxInk"/>.
/// </summary>
public static class Ink
{
    public const int MaxInk = 10;

    public static int Get(Player? player) =>
        player?.PlayerCombatState?.Stars ?? 0;

    public static int Get(Creature? creature) => Get(creature?.Player);

    public static async Task Gain(
        PlayerChoiceContext choiceContext,
        Player owner,
        int amount,
        CardModel? cardSource = null)
    {
        if (amount <= 0 || owner is null)
            return;

        var current = Get(owner);
        var room = MaxInk - current;
        if (room <= 0)
            return;

        var granted = Math.Min(amount, room);
        await PlayerCmd.GainStars(granted, owner);
    }

    /// <summary>
    /// Manual spend for non-card effects (e.g. WorldSeal X-cost). Card seal costs
    /// are paid by the game via <c>CanonicalStarCost</c> — do not call this from
    /// normal seal OnPlay or you double-charge.
    /// </summary>
    public static async Task<bool> TrySpend(
        PlayerChoiceContext choiceContext,
        Player owner,
        int amount,
        CardModel? cardSource = null)
    {
        if (amount <= 0)
            return true;
        if (owner is null || Get(owner) < amount)
            return false;

        await PlayerCmd.LoseStars(amount, owner);
        return true;
    }

    public static bool CanAfford(Player? owner, int amount)
    {
        if (amount <= 0)
            return true;
        return Get(owner) >= amount;
    }
}
