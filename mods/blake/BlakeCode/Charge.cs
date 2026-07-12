using System.Threading.Tasks;
using Blake.BlakeCode.Powers;
using Blake.BlakeCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Blake.BlakeCode;

/// <summary>
/// Blake's fist meter. All growth is multiplicative (Rev = double).
/// Interrupt halves on clean hits; floor is <see cref="GetBase"/>.
/// </summary>
public static class Charge
{
    public const int DefaultBase = 3;

    /// <summary>Per-player base for this combat (raised by Champion's Fist, relics, etc.).</summary>
    private static readonly Dictionary<int, int> CombatBase = new();

    /// <summary>Unleashes this combat (Muscle Memory scaling).</summary>
    private static readonly Dictionary<int, int> CombatUnleashes = new();

    /// <summary>Whether an Unleash happened on the current player turn (YES! refund).</summary>
    private static readonly Dictionary<int, bool> UnleashedThisTurn = new();

    /// <summary>Whether Charge should persist into the next combat (Trophy Belt).</summary>
    private static readonly Dictionary<int, int> PersistedCharge = new();

    private static int Key(Player player) => player.Creature?.GetHashCode() ?? player.GetHashCode();

    public static int GetBase(Player? player)
    {
        if (player is null) return DefaultBase;
        return CombatBase.GetValueOrDefault(Key(player), DefaultBase);
    }

    public static void SetBase(Player player, int value)
    {
        CombatBase[Key(player)] = Math.Max(1, value);
    }

    public static void AddBase(Player player, int delta) =>
        SetBase(player, GetBase(player) + delta);

    public static int Get(Creature? creature) =>
        creature?.GetPower<ChargePower>()?.Amount ?? 0;

    public static int Get(Player? player) => Get(player?.Creature);

    public static int UnleashCount(Player? player)
    {
        if (player is null) return 0;
        return CombatUnleashes.GetValueOrDefault(Key(player), 0);
    }

    public static bool DidUnleashThisTurn(Player? player)
    {
        if (player is null) return false;
        return UnleashedThisTurn.GetValueOrDefault(Key(player), false);
    }

    public static bool HasSuperArmor(Creature? creature) =>
        creature?.GetPower<SuperArmorPower>() is not null;

    public static async Task Ensure(
        PlayerChoiceContext choiceContext,
        Player owner,
        CardModel? cardSource = null)
    {
        if (owner.Creature is null) return;
        if (owner.Creature.GetPower<ChargePower>() is not null) return;

        var start = DefaultBase;
        var key = Key(owner);
        if (PersistedCharge.TryGetValue(key, out var persisted) && persisted > 0)
        {
            start = persisted;
            PersistedCharge.Remove(key);
        }
        else
        {
            start = GetBase(owner);
        }

        await PowerCmd.Apply<ChargePower>(
            choiceContext,
            owner.Creature,
            start,
            owner.Creature,
            cardSource);
    }

    public static async Task Rev(
        PlayerChoiceContext choiceContext,
        Player owner,
        int times = 1,
        CardModel? cardSource = null)
    {
        if (owner.Creature is null || times <= 0) return;
        await Ensure(choiceContext, owner, cardSource);

        var power = owner.Creature.GetPower<ChargePower>();
        if (power is null) return;

        var amount = power.Amount;
        for (var i = 0; i < times; i++)
            amount = Math.Max(amount * 2, GetBase(owner));

        var delta = amount - power.Amount;
        if (delta != 0)
            await PowerCmd.ModifyAmount(choiceContext, power, delta, owner.Creature, cardSource);

        // Pit Crew: first Rev each turn → Block
        var pitCrew = owner.GetRelic<PitCrew>();
        if (pitCrew is not null)
            await pitCrew.OnRev(choiceContext);

        // Heat Haze: charging deals AOE
        var haze = owner.Creature.GetPower<HeatHazePower>();
        if (haze is not null && haze.Amount > 0)
        {
            for (var i = 0; i < times; i++)
            {
                await CreatureCmd.Damage(
                    choiceContext,
                    owner.Creature.CombatState?.HittableEnemies ?? [],
                    haze.Amount,
                    MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered,
                    owner.Creature,
                    cardSource);
            }
        }
    }

    /// <summary>
    /// Deal Charge damage and reset to base (unless <paramref name="reset"/> is false).
    /// Returns the damage dealt (pre-modifiers display value = Charge amount used).
    /// </summary>
    public static async Task<int> Unleash(
        PlayerChoiceContext choiceContext,
        Player owner,
        CardModel? cardSource = null,
        bool reset = true,
        decimal multiplier = 1m,
        int flatBonus = 0)
    {
        if (owner.Creature is null) return 0;
        await Ensure(choiceContext, owner, cardSource);

        var power = owner.Creature.GetPower<ChargePower>();
        if (power is null) return 0;

        var spent = power.Amount;
        var key = Key(owner);
        CombatUnleashes[key] = CombatUnleashes.GetValueOrDefault(key, 0) + 1;
        UnleashedThisTurn[key] = true;

        // Highlight Reel refund
        var reel = owner.Creature.GetPower<HighlightReelPower>();
        if (reel is not null)
        {
            await PlayerCmd.GainEnergy(reel.Amount, owner);
            await CardPileCmd.Draw(choiceContext, reel.Amount, owner);
        }

        if (reset)
        {
            var floor = GetBase(owner);
            var delta = floor - power.Amount;
            if (delta != 0)
                await PowerCmd.ModifyAmount(choiceContext, power, delta, owner.Creature, cardSource);
        }

        return (int)Math.Floor(spent * multiplier) + flatBonus;
    }

    /// <summary>Halve Charge on clean hit; never below base.</summary>
    public static async Task Interrupt(
        PlayerChoiceContext choiceContext,
        Creature ownerCreature,
        CardModel? cardSource = null)
    {
        if (HasSuperArmor(ownerCreature)) return;

        var power = ownerCreature.GetPower<ChargePower>();
        if (power is null) return;

        var player = ownerCreature.Player;
        var floor = player is not null ? GetBase(player) : DefaultBase;
        var halved = Math.Max(floor, power.Amount / 2);
        var delta = halved - power.Amount;
        if (delta == 0) return;

        await PowerCmd.ModifyAmount(choiceContext, power, delta, ownerCreature, cardSource);
    }

    public static void OnPlayerTurnStart(Player player)
    {
        UnleashedThisTurn[Key(player)] = false;
    }

    public static void OnCombatEnd(Player player, bool persist)
    {
        var key = Key(player);
        if (persist)
        {
            var current = Get(player);
            if (current > 0)
                PersistedCharge[key] = current;
        }

        CombatBase.Remove(key);
        CombatUnleashes.Remove(key);
        UnleashedThisTurn.Remove(key);
    }

    public static void ResetCombatTracking(Player player)
    {
        var key = Key(player);
        CombatBase[key] = DefaultBase;
        CombatUnleashes[key] = 0;
        UnleashedThisTurn[key] = false;
    }

    /// <summary>Combo N: true if this card is at least the Nth played this turn (1-based).</summary>
    public static bool IsCombo(CardModel card, int n)
    {
        // CurrentPlayIndex is 0-based within the turn's play sequence.
        return card.CurrentPlayIndex + 1 >= n;
    }

    public static bool IntendsToAttack(Creature? enemy) =>
        enemy?.Monster?.IntendsToAttack == true;
}
