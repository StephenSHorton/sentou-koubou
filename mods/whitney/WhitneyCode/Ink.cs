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
/// No hard bank cap — spend freely on seals and X-cost ink dumps.
/// </summary>
public static class Ink
{
    /// <summary>
    /// Design anchor for cards that scale on "empty palette" (e.g. Negative Space).
    /// Not a hard gain cap.
    /// </summary>
    public const int PaletteReference = 10;

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

        await PlayerCmd.GainStars(amount, owner);
    }

    /// <summary>
    /// Manual spend for non-card effects. Card seal costs are paid by the game via
    /// <c>CanonicalStarCost</c> / star-X; do not double-charge from OnPlay.
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
