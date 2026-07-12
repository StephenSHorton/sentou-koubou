#!/usr/bin/env python3
"""Emit curated Brennen card pool redesign (~4 basic + 10C + 14U + 10R)."""
from __future__ import annotations

import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BRENNEN = ROOT / "mods" / "brennen"
CODE = BRENNEN / "BrennenCode"
CARDS = CODE / "Cards"
POWERS = CODE / "Powers"
RELICS = CODE / "Relics"
LOC_CARDS = BRENNEN / "Brennen" / "localization" / "eng" / "cards.json"
LOC_POWERS = BRENNEN / "Brennen" / "localization" / "eng" / "powers.json"
LOC_RELICS = BRENNEN / "Brennen" / "localization" / "eng" / "relics.json"

KEEP_CARDS = {
    # Basic
    "StrikeBrennen", "DefendBrennen", "LastHit", "Flash",
    # Common
    "Trade", "Poke", "Gank", "ClearTheWave", "Facecheck",
    "Ward", "Back", "TrashTalk", "PowerFarm", "Kite",
    # Uncommon
    "FirstBlood", "Smite", "FarmedUp", "ObjectiveHerald", "ObjectiveDragon",
    "Snowball", "MentalBoom", "ForTheTeam", "TowerDive", "Shutdown",
    "FlashEngage", "Tp", "Outplay", "Peel",
    # Rare
    "Pentakill", "PentaSecure", "OneVNine", "Backdoor", "GuardianAngel",
    "RunItDown", "MainCharacter", "Bounty", "Fountain", "Diff",
}

KEEP_POWERS = {
    "BrennenPower",
    "FedPower",
    "SnowballPower",
    "BountyPower",
    "MainCharacterPower",
    "GuardianAngelPower",
    "MentalBoomPower",
    "ForTheTeamPower",
    "PentaSecurePower",
}

USINGS = """\
using BaseLib.Utils;
using Brennen.BrennenCode;
using Brennen.BrennenCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
"""


def write(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content.strip() + "\n", encoding="utf-8")
    print("wrote", path.relative_to(ROOT))


def loc_key(name: str) -> str:
    snake = []
    for i, ch in enumerate(name):
        if ch.isupper() and i:
            snake.append("_")
        snake.append(ch.upper())
    return f"BRENNEN-{''.join(snake)}"


def emit_systems() -> None:
    write(CODE / "Tilted.cs", """
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Brennen.BrennenCode;

/// <summary>Tilted while at or below 50% max HP.</summary>
public static class Tilted
{
    public static bool IsTilted(Creature? creature)
    {
        if (creature is null || creature.MaxHp <= 0)
            return false;
        return creature.CurrentHp * 2 <= creature.MaxHp;
    }

    public static bool IsTilted(Player? player) => IsTilted(player?.Creature);
}
""")

    write(CODE / "BrennenTurnState.cs", """
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Brennen.BrennenCode;

/// <summary>
/// Per-player-turn counters for Brennen kit (reset by Duo Queue).
/// CardsPlayed / AttacksPlayed count completed plays this turn.
/// </summary>
public static class BrennenTurnState
{
    public static int CardsPlayedThisTurn { get; private set; }
    public static int AttacksPlayedThisTurn { get; private set; }

    public static void ResetTurn()
    {
        CardsPlayedThisTurn = 0;
        AttacksPlayedThisTurn = 0;
    }

    public static void OnCardPlayed(CardModel card)
    {
        CardsPlayedThisTurn++;
        if (card.Type == CardType.Attack)
            AttacksPlayedThisTurn++;
    }
}
""")

    write(POWERS / "FedPower.cs", """
using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Brennen.BrennenCode.Powers;

/// <summary>Fed stacks never drop during combat. Scales snowball payoffs.</summary>
public sealed class FedPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Fed",
            "Your snowball. Never decreases during combat.",
            "You have {Amount} [gold]Fed[/gold].");
}
""")

    write(CODE / "Fed.cs", """
using System.Threading.Tasks;
using Brennen.BrennenCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode;

/// <summary>Helpers for Brennen's Fed counter.</summary>
public static class Fed
{
    public static int Get(Player? player) =>
        player?.Creature?.GetPower<FedPower>()?.Amount ?? 0;

    public static async Task Gain(
        PlayerChoiceContext choiceContext,
        Player owner,
        int amount,
        CardModel? cardSource = null)
    {
        if (amount <= 0 || owner.Creature is null)
            return;

        // Bounty: double Fed gains while Tilted.
        if (Tilted.IsTilted(owner) && owner.Creature.GetPower<BountyPower>() is not null)
            amount *= 2;

        await PowerCmd.Apply<FedPower>(
            choiceContext, owner.Creature, amount, owner.Creature, cardSource);

        // Snowball: on Fed gain → Block + draw 1
        var snow = owner.Creature.GetPower<SnowballPower>();
        if (snow is not null && snow.Amount > 0)
        {
            snow.Flash();
            await CreatureCmd.GainBlock(owner.Creature, snow.Amount, ValueProp.Unpowered, null);
            await CardPileCmd.Draw(choiceContext, 1, owner);
        }
    }
}
""")

    write(POWERS / "SnowballPower.cs", """
using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Brennen.BrennenCode.Powers;

/// <summary>Whenever you gain Fed, gain Amount Block and draw 1 (wired via Fed.Gain).</summary>
public sealed class SnowballPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Snowball",
            "Whenever you gain [gold]Fed[/gold], gain {Amount} [gold]Block[/gold] and draw 1 card.",
            "Whenever you gain [gold]Fed[/gold], gain {Amount} [gold]Block[/gold] and draw 1 card.");
}
""")

    write(POWERS / "BountyPower.cs", """
using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Brennen.BrennenCode.Powers;

/// <summary>Fed gains are doubled while Tilted (logic in Fed.Gain).</summary>
public sealed class BountyPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Bounty",
            "[gold]Fed[/gold] you gain is doubled while [gold]Tilted[/gold].",
            "[gold]Fed[/gold] you gain is doubled while [gold]Tilted[/gold].");
}
""")

    write(POWERS / "MainCharacterPower.cs", """
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Brennen.BrennenCode.Powers;

/// <summary>While Tilted, gain 1 Energy at start of turn.</summary>
public sealed class MainCharacterPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Main Character",
            "While [gold]Tilted[/gold], gain 1 Energy at the start of your turn.",
            "While [gold]Tilted[/gold], gain 1 Energy at the start of your turn.");

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner))
            return;
        if (Owner.Player is null || !Tilted.IsTilted(Owner.Player))
            return;
        Flash();
        await PlayerCmd.GainEnergy(1, Owner.Player);
    }
}
""")

    write(POWERS / "GuardianAngelPower.cs", """
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Brennen.BrennenCode.Powers;

/// <summary>First lethal hit: heal to 25% max HP instead, then remove.</summary>
public sealed class GuardianAngelPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Guardian Angel",
            "The first time you would die, heal to [blue]25%[/blue] of your max HP instead.",
            "The first time you would die, heal to [blue]25%[/blue] of your max HP instead.");

    public override bool ShouldDie(Creature creature)
    {
        if (creature != Owner)
            return true;
        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner)
            return;
        Flash();
        var targetHp = System.Math.Max(1, creature.MaxHp / 4);
        var heal = targetHp - creature.CurrentHp;
        if (heal > 0)
            await CreatureCmd.Heal(creature, heal);
        await PowerCmd.Remove(this);
    }
}
""")

    write(POWERS / "MentalBoomPower.cs", """
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Powers;

/// <summary>Lose HP on your turn → draw.</summary>
public sealed class MentalBoomPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Mental Boom",
            "Whenever you lose HP during your turn, draw {Amount} card(s).",
            "Whenever you lose HP during your turn, draw {Amount} card(s).");

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0)
            return;
        if (CombatState is null || CombatState.CurrentSide != Owner.Side)
            return;
        if (Owner.Player is null)
            return;
        Flash();
        await CardPileCmd.Draw(choiceContext, Amount, Owner.Player);
    }
}
""")

    write(POWERS / "ForTheTeamPower.cs", """
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Powers;

/// <summary>Lose HP on your turn → Vigor.</summary>
public sealed class ForTheTeamPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "For the Team",
            "Whenever you lose HP during your turn, gain {Amount} [gold]Vigor[/gold].",
            "Whenever you lose HP during your turn, gain {Amount} [gold]Vigor[/gold].");

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0)
            return;
        if (CombatState is null || CombatState.CurrentSide != Owner.Side)
            return;
        Flash();
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner, Amount, Owner, cardSource);
    }
}
""")

    write(POWERS / "PentaSecurePower.cs", """
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Brennen.BrennenCode.Powers;

/// <summary>
/// 5th Attack each turn plays twice via ModifyCardPlayCount.
/// Amount >= 2: also gain 1 Fed when the double triggers.
/// </summary>
public sealed class PentaSecurePower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Penta Secure",
            "Whenever you play your 5th Attack in a single turn, that Attack triggers twice.",
            "Whenever you play your 5th Attack in a single turn, that Attack triggers twice.");

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner?.Creature != Owner)
            return playCount;
        if (card.Type != CardType.Attack)
            return playCount;
        // AttacksPlayedThisTurn counts completed plays; 4 means this is the 5th.
        if (BrennenTurnState.AttacksPlayedThisTurn != 4)
            return playCount;
        Flash();
        return playCount + 1;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
            return;
        if (cardPlay.Card.Type != CardType.Attack)
            return;
        // After the 5th attack completes, optional Fed for upgraded power (Amount >= 2).
        if (BrennenTurnState.AttacksPlayedThisTurn == 5 && Amount >= 2 && Owner.Player is not null)
            await Fed.Gain(choiceContext, Owner.Player, 1, cardPlay.Card);
    }
}
""")

    write(RELICS / "DuoQueue.cs", """
using System.Collections.Generic;
using System.Threading.Tasks;
using Brennen.BrennenCode;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Relics;

/// <summary>Starter: whenever you kill an enemy, gain 1 Fed. Tracks turn play counts.</summary>
public sealed class DuoQueue : BrennenRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner)
            return;
        BrennenTurnState.ResetTurn();
        await Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner)
            return;
        BrennenTurnState.OnCardPlayed(cardPlay.Card);
        await Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner is null || dealer != Owner.Creature)
            return;
        if (target is null || !result.WasTargetKilled)
            return;
        if (Owner.Creature is null || target.Side == Owner.Creature.Side)
            return;
        Flash();
        await Fed.Gain(choiceContext, Owner, 1);
    }
}
""")


def card_file(rarity: str, name: str, body: str) -> None:
    write(CARDS / rarity / f"{name}.cs", body)


def emit_basic() -> None:
    card_file("Basic", "StrikeBrennen", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Basic;

public sealed class StrikeBrennen() : BrennenCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Damage.UpgradeValueBy(3m);
    }}
}}
""")

    card_file("Basic", "DefendBrennen", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Basic;

public sealed class DefendBrennen() : BrennenCard(1, CardType.Skill, CardRarity.Basic, TargetType.None)
{{
    public override bool GainsBlock => true;

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(5, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardBlock(this, play);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Block.UpgradeValueBy(3m);
    }}
}}
""")

    card_file("Basic", "LastHit", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Basic;

/// <summary>Secure the kill — Fatal: Fed + draw.</summary>
public sealed class LastHit() : BrennenCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (play.Target is not null && play.Target.IsDead)
        {{
            await Fed.Gain(choiceContext, Owner, 1, this);
            await CardPileCmd.Draw(choiceContext, 1, Owner);
        }}
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Damage.UpgradeValueBy(3m);
    }}
}}
""")

    card_file("Basic", "Flash", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Basic;

/// <summary>0-cost escape — Block, Retain, Exhaust.</summary>
public sealed class Flash() : BrennenCard(0, CardType.Skill, CardRarity.Basic, TargetType.None)
{{
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(4, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardBlock(this, play);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Block.UpgradeValueBy(3m);
    }}
}}
""")


def emit_commons() -> None:
    card_file("Common", "Trade", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Common;

public sealed class Trade() : BrennenCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(10, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (Owner.Creature is not null)
        {{
            await CreatureCmd.Damage(
                choiceContext,
                [Owner.Creature],
                2,
                ValueProp.Unblockable | ValueProp.Unpowered,
                Owner.Creature,
                this);
        }}
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Damage.UpgradeValueBy(3m);
    }}
}}
""")

    card_file("Common", "Poke", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Common;

public sealed class Poke() : BrennenCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3, ValueProp.Move),
        new CardsVar(1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Damage.UpgradeValueBy(2m);
    }}
}}
""")

    card_file("Common", "Gank", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Common;

public sealed class Gank() : BrennenCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5, ValueProp.Move),
        new RepeatVar(2),
        new EnergyVar(1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardAttack(this, play)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .Execute(choiceContext);
        if (play.Target is not null && play.Target.IsDead)
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Damage.UpgradeValueBy(2m);
    }}
}}
""")

    card_file("Common", "ClearTheWave", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Common;

public sealed class ClearTheWave() : BrennenCard(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Damage.UpgradeValueBy(3m);
    }}
}}
""")

    card_file("Common", "Facecheck", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Common;

public sealed class Facecheck() : BrennenCard(1, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
{{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(13, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        var combat = Owner.Creature?.CombatState;
        if (combat is not null && combat.HittableEnemies.Count() >= 2 && Owner.Creature is not null)
        {{
            await CreatureCmd.Damage(
                choiceContext,
                [Owner.Creature],
                3,
                ValueProp.Unblockable | ValueProp.Unpowered,
                Owner.Creature,
                this);
        }}
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Damage.UpgradeValueBy(4m);
    }}
}}
""")

    card_file("Common", "Ward", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Common;

public sealed class Ward() : BrennenCard(1, CardType.Skill, CardRarity.Common, TargetType.None)
{{
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(6, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardBlock(this, play);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Block.UpgradeValueBy(3m);
    }}
}}
""")

    card_file("Common", "Back", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Common;

public sealed class Back() : BrennenCard(1, CardType.Skill, CardRarity.Common, TargetType.None)
{{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5, ValueProp.Move),
        new EnergyVar(1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardBlock(this, play);
        if (Owner.Creature is not null)
        {{
            await PowerCmd.Apply<EnergyNextTurnPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars.Energy.IntValue,
                Owner.Creature,
                this);
        }}
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Block.UpgradeValueBy(3m);
    }}
}}
""")

    card_file("Common", "TrashTalk", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Common;

public sealed class TrashTalk() : BrennenCard(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<FrailPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Weak", 1),
        new DynamicVar("Frail", 1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        if (play.Target is null)
            return;
        await PowerCmd.Apply<WeakPower>(
            choiceContext, play.Target, DynamicVars["Weak"].IntValue, Owner.Creature, this);
        await PowerCmd.Apply<FrailPower>(
            choiceContext, play.Target, DynamicVars["Frail"].IntValue, Owner.Creature, this);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars["Weak"].UpgradeValueBy(1m);
        DynamicVars["Frail"].UpgradeValueBy(1m);
    }}
}}
""")

    card_file("Common", "PowerFarm", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Common;

public sealed class PowerFarm() : BrennenCard(1, CardType.Skill, CardRarity.Common, TargetType.None)
{{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VigorPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Vigor", 3),
        new CardsVar(1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        if (Owner.Creature is not null)
        {{
            await PowerCmd.Apply<VigorPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["Vigor"].IntValue,
                Owner.Creature,
                this);
        }}
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars["Vigor"].UpgradeValueBy(2m);
    }}
}}
""")

    card_file("Common", "Kite", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Common;

public sealed class Kite() : BrennenCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6, ValueProp.Move),
        new BlockVar(4, ValueProp.Move),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CommonActions.CardBlock(this, play);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }}
}}
""")


def emit_uncommons() -> None:
    card_file("Uncommon", "FirstBlood", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Uncommon;

public sealed class FirstBlood() : BrennenCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4, ValueProp.Move),
        new EnergyVar(1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (play.Target is not null && play.Target.IsDead)
        {{
            await Fed.Gain(choiceContext, Owner, 2, this);
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            await CardPileCmd.Draw(choiceContext, 1, Owner);
        }}
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Damage.UpgradeValueBy(3m);
    }}
}}
""")

    card_file("Uncommon", "Smite", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Uncommon;

public sealed class Smite() : BrennenCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12, ValueProp.Unblockable | ValueProp.Move),
        new EnergyVar(1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (play.Target is not null && play.Target.IsDead)
        {{
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            if (IsUpgraded)
                await Fed.Gain(choiceContext, Owner, 1, this);
        }}
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Damage.UpgradeValueBy(4m);
    }}
}}
""")

    card_file("Uncommon", "FarmedUp", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Uncommon;

public sealed class FarmedUp() : BrennenCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5, ValueProp.Move),
        new DynamicVar("FedScale", 3),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        var fed = Fed.Get(Owner);
        var dmg = DynamicVars.Damage.BaseValue + DynamicVars["FedScale"].BaseValue * fed;
        var stored = DynamicVars.Damage.BaseValue;
        DynamicVars.Damage.BaseValue = dmg;
        try
        {{
            await CommonActions.CardAttack(this, play).Execute(choiceContext);
        }}
        finally
        {{
            DynamicVars.Damage.BaseValue = stored;
        }}
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars["FedScale"].UpgradeValueBy(1m);
    }}
}}
""")

    card_file("Uncommon", "ObjectiveHerald", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Uncommon;

public sealed class ObjectiveHerald() : BrennenCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(8, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardBlock(this, play);
        await Fed.Gain(choiceContext, Owner, 1, this);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Block.UpgradeValueBy(4m);
    }}
}}
""")

    card_file("Uncommon", "ObjectiveDragon", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Uncommon;

public sealed class ObjectiveDragon() : BrennenCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VigorPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("FedGain", 1),
        new DynamicVar("Vigor", 3),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await Fed.Gain(choiceContext, Owner, DynamicVars["FedGain"].IntValue, this);
        if (Owner.Creature is not null)
        {{
            await PowerCmd.Apply<VigorPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["Vigor"].IntValue,
                Owner.Creature,
                this);
        }}
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars["FedGain"].UpgradeValueBy(1m);
    }}
}}
""")

    card_file("Uncommon", "Snowball", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Uncommon;

public sealed class Snowball() : BrennenCard(1, CardType.Power, CardRarity.Uncommon, TargetType.None)
{{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Snowball", 4)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        if (Owner.Creature is not null)
        {{
            await PowerCmd.Apply<SnowballPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["Snowball"].IntValue,
                Owner.Creature,
                this);
        }}
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars["Snowball"].UpgradeValueBy(2m);
    }}
}}
""")

    card_file("Uncommon", "MentalBoom", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Uncommon;

public sealed class MentalBoom() : BrennenCard(1, CardType.Power, CardRarity.Uncommon, TargetType.None)
{{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("MentalBoom", 1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        if (Owner.Creature is not null)
        {{
            await PowerCmd.Apply<MentalBoomPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["MentalBoom"].IntValue,
                Owner.Creature,
                this);
        }}
    }}

    protected override void OnUpgrade()
    {{
        EnergyCost.UpgradeBy(-1);
    }}
}}
""")

    card_file("Uncommon", "ForTheTeam", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Uncommon;

public sealed class ForTheTeam() : BrennenCard(1, CardType.Power, CardRarity.Uncommon, TargetType.None)
{{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VigorPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("ForTheTeam", 2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        if (Owner.Creature is not null)
        {{
            await PowerCmd.Apply<ForTheTeamPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["ForTheTeam"].IntValue,
                Owner.Creature,
                this);
        }}
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars["ForTheTeam"].UpgradeValueBy(1m);
    }}
}}
""")

    card_file("Uncommon", "TowerDive", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Uncommon;

public sealed class TowerDive() : BrennenCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(18, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        var killed = play.Target is not null && play.Target.IsDead;
        if (!killed && Owner.Creature is not null)
        {{
            await CreatureCmd.Damage(
                choiceContext,
                [Owner.Creature],
                4,
                ValueProp.Unblockable | ValueProp.Unpowered,
                Owner.Creature,
                this);
        }}
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Damage.UpgradeValueBy(6m);
    }}
}}
""")

    card_file("Uncommon", "Shutdown", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Uncommon;

public sealed class Shutdown() : BrennenCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9, ValueProp.Move),
        new DamageVar("TiltedDamage", 18, ValueProp.Move),
    ];

    protected override bool ShouldGlowGoldInternal => Tilted.IsTilted(Owner);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        var dmg = Tilted.IsTilted(Owner)
            ? DynamicVars["TiltedDamage"].BaseValue
            : DynamicVars.Damage.BaseValue;
        var stored = DynamicVars.Damage.BaseValue;
        DynamicVars.Damage.BaseValue = dmg;
        try
        {{
            await CommonActions.CardAttack(this, play).Execute(choiceContext);
        }}
        finally
        {{
            DynamicVars.Damage.BaseValue = stored;
        }}

        if (Tilted.IsTilted(Owner) && play.Target is not null && play.Target.IsDead)
            await Fed.Gain(choiceContext, Owner, 2, this);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["TiltedDamage"].UpgradeValueBy(4m);
    }}
}}
""")

    card_file("Uncommon", "FlashEngage", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Uncommon;

public sealed class FlashEngage() : BrennenCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VulnerablePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move),
        new DynamicVar("Vulnerable", 2),
        new EnergyVar(1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        var first = BrennenTurnState.CardsPlayedThisTurn == 0;
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (play.Target is not null)
        {{
            await PowerCmd.Apply<VulnerablePower>(
                choiceContext,
                play.Target,
                DynamicVars["Vulnerable"].IntValue,
                Owner.Creature,
                this);
        }}
        if (first)
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Damage.UpgradeValueBy(3m);
    }}
}}
""")

    card_file("Uncommon", "Tp", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Uncommon;

public sealed class Tp() : BrennenCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(2),
        new CardsVar(1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        if (Owner.Creature is null)
            return;
        await PowerCmd.Apply<EnergyNextTurnPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars.Energy.IntValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<DrawCardsNextTurnPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars.Cards.IntValue,
            Owner.Creature,
            this);
    }}

    protected override void OnUpgrade()
    {{
        EnergyCost.UpgradeBy(-1);
    }}
}}
""")

    card_file("Uncommon", "Outplay", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Uncommon;

public sealed class Outplay() : BrennenCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8, ValueProp.Move),
        new CardsVar(2),
    ];

    protected override bool ShouldGlowGoldInternal => Tilted.IsTilted(Owner);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardBlock(this, play);
        if (Tilted.IsTilted(Owner))
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Block.UpgradeValueBy(3m);
    }}
}}
""")

    card_file("Uncommon", "Peel", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Uncommon;

public sealed class Peel() : BrennenCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7, ValueProp.Move),
        new DynamicVar("Weak", 2),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardBlock(this, play);
        if (play.Target is not null)
        {{
            await PowerCmd.Apply<WeakPower>(
                choiceContext,
                play.Target,
                DynamicVars["Weak"].IntValue,
                Owner.Creature,
                this);
        }}
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars["Weak"].UpgradeValueBy(1m);
    }}
}}
""")


def emit_rares() -> None:
    card_file("Rare", "Pentakill", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Rare;

/// <summary>AoE that discounts by Attacks played this turn (max 3).</summary>
public sealed class Pentakill() : BrennenCard(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(9, ValueProp.Move)];

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {{
        if (card != this)
        {{
            modifiedCost = originalCost;
            return false;
        }}
        var reduce = System.Math.Min(BrennenTurnState.AttacksPlayedThisTurn, 3);
        modifiedCost = System.Math.Max(0, originalCost - reduce);
        return reduce > 0;
    }}

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Damage.UpgradeValueBy(3m);
    }}
}}
""")

    card_file("Rare", "PentaSecure", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Rare;

public sealed class PentaSecure() : BrennenCard(1, CardType.Power, CardRarity.Rare, TargetType.None)
{{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Penta", 1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        if (Owner.Creature is not null)
        {{
            await PowerCmd.Apply<PentaSecurePower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["Penta"].IntValue,
                Owner.Creature,
                this);
        }}
    }}

    protected override void OnUpgrade()
    {{
        // Amount 2: also Fed.Gain(1) when the 5th-attack double triggers.
        DynamicVars["Penta"].UpgradeValueBy(1m);
    }}
}}
""")

    card_file("Rare", "OneVNine", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Rare;

public sealed class OneVNine() : BrennenCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(22, ValueProp.Move)];

    protected override bool ShouldGlowGoldInternal => Tilted.IsTilted(Owner);

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {{
        if (card != this || !Tilted.IsTilted(Owner))
        {{
            modifiedCost = originalCost;
            return false;
        }}
        modifiedCost = 0;
        return true;
    }}

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Damage.UpgradeValueBy(8m);
    }}
}}
""")

    card_file("Rare", "Backdoor", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Rare;

/// <summary>Huge hit — only if you have not played an Attack yet this turn.</summary>
public sealed class Backdoor() : BrennenCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(32, ValueProp.Move)];

    protected override bool IsPlayable =>
        base.IsPlayable && BrennenTurnState.AttacksPlayedThisTurn == 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        if (BrennenTurnState.AttacksPlayedThisTurn != 0)
            return;
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Damage.UpgradeValueBy(8m);
    }}
}}
""")

    card_file("Rare", "GuardianAngel", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Rare;

public sealed class GuardianAngel() : BrennenCard(2, CardType.Power, CardRarity.Rare, TargetType.None)
{{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        if (Owner.Creature is not null)
        {{
            await PowerCmd.Apply<GuardianAngelPower>(
                choiceContext,
                Owner.Creature,
                1,
                Owner.Creature,
                this);
        }}
    }}

    protected override void OnUpgrade()
    {{
        EnergyCost.UpgradeBy(-1);
    }}
}}
""")

    card_file("Rare", "RunItDown", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Rare;

public sealed class RunItDown() : BrennenCard(0, CardType.Skill, CardRarity.Rare, TargetType.None)
{{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(2),
        new CardsVar(2),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        if (Owner.Creature is not null)
        {{
            await CreatureCmd.Damage(
                choiceContext,
                [Owner.Creature],
                7,
                ValueProp.Unblockable | ValueProp.Unpowered,
                Owner.Creature,
                this);
        }}
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        if (Tilted.IsTilted(Owner))
            await Fed.Gain(choiceContext, Owner, 1, this);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Cards.UpgradeValueBy(1m);
    }}
}}
""")

    card_file("Rare", "MainCharacter", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Rare;

public sealed class MainCharacter() : BrennenCard(2, CardType.Power, CardRarity.Rare, TargetType.None)
{{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        if (Owner.Creature is not null)
        {{
            await PowerCmd.Apply<MainCharacterPower>(
                choiceContext,
                Owner.Creature,
                1,
                Owner.Creature,
                this);
        }}
    }}

    protected override void OnUpgrade()
    {{
        EnergyCost.UpgradeBy(-1);
    }}
}}
""")

    card_file("Rare", "Bounty", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Rare;

public sealed class Bounty() : BrennenCard(1, CardType.Power, CardRarity.Rare, TargetType.None)
{{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        if (Owner.Creature is not null)
        {{
            await PowerCmd.Apply<BountyPower>(
                choiceContext,
                Owner.Creature,
                1,
                Owner.Creature,
                this);
        }}
    }}

    protected override void OnUpgrade()
    {{
        EnergyCost.UpgradeBy(-1);
    }}
}}
""")

    card_file("Rare", "Fountain", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Rare;

public sealed class Fountain() : BrennenCard(2, CardType.Skill, CardRarity.Rare, TargetType.None)
{{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("HealPerFed", 3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        if (Owner.Creature is null)
            return;
        var heal = DynamicVars["HealPerFed"].IntValue * Fed.Get(Owner);
        if (heal > 0)
            await CreatureCmd.Heal(Owner.Creature, heal);
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars["HealPerFed"].UpgradeValueBy(1m);
    }}
}}
""")

    card_file("Rare", "Diff", f"""
{USINGS}
namespace Brennen.BrennenCode.Cards.Rare;

/// <summary>
/// MVP hybrid of Choose-one Diff modes (no modal API used):
/// Fed + Block + draw + Weak ALL — flex peel/setup package.
/// TODO: replace with true 4-option choice UI when available.
/// </summary>
public sealed class Diff() : BrennenCard(1, CardType.Skill, CardRarity.Rare, TargetType.None)
{{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6, ValueProp.Move),
        new CardsVar(2),
        new DynamicVar("Weak", 1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await Fed.Gain(choiceContext, Owner, 1, this);
        await CommonActions.CardBlock(this, play);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        var combat = Owner.Creature?.CombatState;
        if (combat is null)
            return;
        foreach (var enemy in combat.HittableEnemies)
        {{
            await PowerCmd.Apply<WeakPower>(
                choiceContext,
                enemy,
                DynamicVars["Weak"].IntValue,
                Owner.Creature,
                this);
        }}
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }}
}}
""")


def cleanup() -> None:
    for rarity in ("Basic", "Common", "Uncommon", "Rare"):
        folder = CARDS / rarity
        if not folder.is_dir():
            continue
        for p in folder.glob("*.cs"):
            if p.stem not in KEEP_CARDS:
                p.unlink()
                print("removed", p.relative_to(ROOT))

    for p in POWERS.glob("*.cs"):
        if p.stem not in KEEP_POWERS:
            p.unlink()
            print("removed", p.relative_to(ROOT))


def emit_brennen_character() -> None:
    path = CODE / "Character" / "Brennen.cs"
    text = path.read_text(encoding="utf-8")
    # Replace starting deck block
    start = text.index("public override IEnumerable<CardModel> StartingDeck =>")
    end = text.index("];", start) + 2
    new_deck = """public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeBrennen>(),
        ModelDb.Card<StrikeBrennen>(),
        ModelDb.Card<StrikeBrennen>(),
        ModelDb.Card<StrikeBrennen>(),
        ModelDb.Card<DefendBrennen>(),
        ModelDb.Card<DefendBrennen>(),
        ModelDb.Card<DefendBrennen>(),
        ModelDb.Card<DefendBrennen>(),
        ModelDb.Card<LastHit>(),
        ModelDb.Card<Flash>(),
    ]"""
    text = text[:start] + new_deck + text[end:]
    # Update summary comment
    text = text.replace(
        """/// <summary>
/// Brennen — frontline tank.
/// Peels, pays HP for tempo, keeps Block (Proxy Camp), slams with Tower Dive.
/// Feed is a reward-pool meme, not a starter.
/// </summary>""",
        """/// <summary>
/// Brennen — Solo-Queue Carry.
/// Fed snowball, Tilted comebacks, self-damage engines. Duo Queue on-kill Fed.
/// Starter: 4 Strike / 4 Defend / Last Hit / Flash.
/// </summary>""",
    )
    path.write_text(text, encoding="utf-8")
    print("updated", path.relative_to(ROOT))


def emit_loc() -> None:
    cards = {
        "BRENNEN-STRIKE_BRENNEN.title": "Strike",
        "BRENNEN-STRIKE_BRENNEN.description": "Deal {Damage:diff()} damage.",
        "BRENNEN-DEFEND_BRENNEN.title": "Defend",
        "BRENNEN-DEFEND_BRENNEN.description": "Gain {Block:diff()} [gold]Block[/gold].",
        "BRENNEN-LAST_HIT.title": "Last Hit",
        "BRENNEN-LAST_HIT.description": "Deal {Damage:diff()} damage.\\nIf Fatal, gain [blue]1[/blue] [gold]Fed[/gold] and draw [blue]1[/blue] card.",
        "BRENNEN-FLASH.title": "Flash",
        "BRENNEN-FLASH.description": "Gain {Block:diff()} [gold]Block[/gold].",
        "BRENNEN-TRADE.title": "Trade",
        "BRENNEN-TRADE.description": "Deal {Damage:diff()} damage.\\nLose [blue]2[/blue] HP.",
        "BRENNEN-POKE.title": "Poke",
        "BRENNEN-POKE.description": "Deal {Damage:diff()} damage.\\nDraw {Cards:diff()} card.",
        "BRENNEN-GANK.title": "Gank",
        "BRENNEN-GANK.description": "Deal {Damage:diff()} damage {Repeat:diff()} times.\\nIf Fatal, gain {Energy:energyIcons()}.",
        "BRENNEN-CLEAR_THE_WAVE.title": "Clear the Wave",
        "BRENNEN-CLEAR_THE_WAVE.description": "Deal {Damage:diff()} damage to ALL enemies.",
        "BRENNEN-FACECHECK.title": "Facecheck",
        "BRENNEN-FACECHECK.description": "Deal {Damage:diff()} damage to a random enemy.\\nIf there are [blue]2[/blue] or more enemies, lose [blue]3[/blue] HP.",
        "BRENNEN-WARD.title": "Ward",
        "BRENNEN-WARD.description": "Gain {Block:diff()} [gold]Block[/gold].",
        "BRENNEN-BACK.title": "Back",
        "BRENNEN-BACK.description": "Gain {Block:diff()} [gold]Block[/gold].\\nNext turn, gain {Energy:energyIcons()}.",
        "BRENNEN-TRASH_TALK.title": "Trash Talk",
        "BRENNEN-TRASH_TALK.description": "Apply {Weak:diff()} [gold]Weak[/gold] and {Frail:diff()} [gold]Frail[/gold].",
        "BRENNEN-POWER_FARM.title": "Power Farm",
        "BRENNEN-POWER_FARM.description": "Gain {Vigor:diff()} [gold]Vigor[/gold].\\nDraw {Cards:diff()} card.",
        "BRENNEN-KITE.title": "Kite",
        "BRENNEN-KITE.description": "Deal {Damage:diff()} damage.\\nGain {Block:diff()} [gold]Block[/gold].",
        "BRENNEN-FIRST_BLOOD.title": "First Blood",
        "BRENNEN-FIRST_BLOOD.description": "Deal {Damage:diff()} damage.\\nIf Fatal, gain [blue]2[/blue] [gold]Fed[/gold], {Energy:energyIcons()}, and draw [blue]1[/blue] card.",
        "BRENNEN-SMITE.title": "Smite",
        "BRENNEN-SMITE.description": "Deal {Damage:diff()} [gold]Unblockable[/gold] damage.\\nIf Fatal, gain {Energy:energyIcons()}.",
        "BRENNEN-FARMED_UP.title": "Farmed Up",
        "BRENNEN-FARMED_UP.description": "Deal damage equal to {Damage:diff()} plus {FedScale:diff()} times your [gold]Fed[/gold].",
        "BRENNEN-OBJECTIVE_HERALD.title": "Objective: Herald",
        "BRENNEN-OBJECTIVE_HERALD.description": "Gain {Block:diff()} [gold]Block[/gold].\\nGain [blue]1[/blue] [gold]Fed[/gold].",
        "BRENNEN-OBJECTIVE_DRAGON.title": "Objective: Dragon",
        "BRENNEN-OBJECTIVE_DRAGON.description": "Gain {FedGain:diff()} [gold]Fed[/gold].\\nGain {Vigor:diff()} [gold]Vigor[/gold].",
        "BRENNEN-SNOWBALL.title": "Snowball",
        "BRENNEN-SNOWBALL.description": "Whenever you gain [gold]Fed[/gold], gain {Snowball:diff()} [gold]Block[/gold] and draw [blue]1[/blue] card.",
        "BRENNEN-MENTAL_BOOM.title": "Mental Boom",
        "BRENNEN-MENTAL_BOOM.description": "Whenever you lose HP during your turn, draw {MentalBoom:diff()} card(s).",
        "BRENNEN-FOR_THE_TEAM.title": "For the Team",
        "BRENNEN-FOR_THE_TEAM.description": "Whenever you lose HP during your turn, gain {ForTheTeam:diff()} [gold]Vigor[/gold].",
        "BRENNEN-TOWER_DIVE.title": "Tower Dive",
        "BRENNEN-TOWER_DIVE.description": "Deal {Damage:diff()} damage.\\nLose [blue]4[/blue] HP.\\nIf Fatal, skip the HP loss.",
        "BRENNEN-SHUTDOWN.title": "Shutdown",
        "BRENNEN-SHUTDOWN.description": "Deal {Damage:diff()} damage.\\nIf [gold]Tilted[/gold], deal {TiltedDamage:diff()} instead.\\nIf [gold]Tilted[/gold] and Fatal, gain [blue]2[/blue] [gold]Fed[/gold].",
        "BRENNEN-FLASH_ENGAGE.title": "Flash Engage",
        "BRENNEN-FLASH_ENGAGE.description": "Deal {Damage:diff()} damage.\\nApply {Vulnerable:diff()} [gold]Vulnerable[/gold].\\nIf this is the first card you play this turn, gain {Energy:energyIcons()}.",
        "BRENNEN-TP.title": "TP",
        "BRENNEN-TP.description": "Next turn, gain {Energy:energyIcons()} and draw {Cards:diff()} card.",
        "BRENNEN-OUTPLAY.title": "Outplay",
        "BRENNEN-OUTPLAY.description": "Gain {Block:diff()} [gold]Block[/gold].\\nIf [gold]Tilted[/gold], draw {Cards:diff()} cards.",
        "BRENNEN-PEEL.title": "Peel",
        "BRENNEN-PEEL.description": "Gain {Block:diff()} [gold]Block[/gold].\\nApply {Weak:diff()} [gold]Weak[/gold].",
        "BRENNEN-PENTAKILL.title": "Pentakill",
        "BRENNEN-PENTAKILL.description": "Deal {Damage:diff()} damage to ALL enemies.\\nCosts [blue]1[/blue] less [gold]Energy[/gold] for each Attack played this turn (min 0).",
        "BRENNEN-PENTA_SECURE.title": "Penta Secure",
        "BRENNEN-PENTA_SECURE.description": "Whenever you play your 5th Attack in a single turn, that Attack triggers twice.",
        "BRENNEN-ONE_V_NINE.title": "1v9",
        "BRENNEN-ONE_V_NINE.description": "Deal {Damage:diff()} damage.\\nIf [gold]Tilted[/gold], costs [blue]0[/blue].",
        "BRENNEN-BACKDOOR.title": "Backdoor",
        "BRENNEN-BACKDOOR.description": "Deal {Damage:diff()} damage.\\nCan only be played if you have not played an Attack this turn.",
        "BRENNEN-GUARDIAN_ANGEL.title": "Guardian Angel",
        "BRENNEN-GUARDIAN_ANGEL.description": "The first time you would die, heal to [blue]25%[/blue] of your max HP instead.",
        "BRENNEN-RUN_IT_DOWN.title": "Run It Down",
        "BRENNEN-RUN_IT_DOWN.description": "Lose [blue]7[/blue] HP.\\nGain {Energy:energyIcons()}.\\nDraw {Cards:diff()} cards.\\nIf you are now [gold]Tilted[/gold], gain [blue]1[/blue] [gold]Fed[/gold].",
        "BRENNEN-MAIN_CHARACTER.title": "Main Character",
        "BRENNEN-MAIN_CHARACTER.description": "While [gold]Tilted[/gold], gain [blue]1[/blue] Energy at the start of your turn.",
        "BRENNEN-BOUNTY.title": "Bounty",
        "BRENNEN-BOUNTY.description": "[gold]Fed[/gold] you gain is doubled while [gold]Tilted[/gold].",
        "BRENNEN-FOUNTAIN.title": "Fountain",
        "BRENNEN-FOUNTAIN.description": "Heal HP equal to {HealPerFed:diff()} times your [gold]Fed[/gold].",
        "BRENNEN-DIFF.title": "Diff",
        "BRENNEN-DIFF.description": "Gain [blue]1[/blue] [gold]Fed[/gold].\\nGain {Block:diff()} [gold]Block[/gold].\\nDraw {Cards:diff()} cards.\\nApply {Weak:diff()} [gold]Weak[/gold] to ALL enemies.",
    }
    # Unescape for real newlines in JSON
    cards = {k: v.replace("\\n", "\n") for k, v in cards.items()}
    write(LOC_CARDS, json.dumps(cards, indent=2, ensure_ascii=False))

    powers = {
        "BRENNEN-FED_POWER.title": "Fed",
        "BRENNEN-FED_POWER.description": "Your snowball. Never decreases during combat. You have {Amount} [gold]Fed[/gold].",
        "BRENNEN-SNOWBALL_POWER.title": "Snowball",
        "BRENNEN-SNOWBALL_POWER.description": "Whenever you gain [gold]Fed[/gold], gain {Amount} [gold]Block[/gold] and draw 1 card.",
        "BRENNEN-BOUNTY_POWER.title": "Bounty",
        "BRENNEN-BOUNTY_POWER.description": "[gold]Fed[/gold] you gain is doubled while [gold]Tilted[/gold].",
        "BRENNEN-MAIN_CHARACTER_POWER.title": "Main Character",
        "BRENNEN-MAIN_CHARACTER_POWER.description": "While [gold]Tilted[/gold], gain 1 Energy at the start of your turn.",
        "BRENNEN-GUARDIAN_ANGEL_POWER.title": "Guardian Angel",
        "BRENNEN-GUARDIAN_ANGEL_POWER.description": "The first time you would die, heal to [blue]25%[/blue] of your max HP instead.",
        "BRENNEN-MENTAL_BOOM_POWER.title": "Mental Boom",
        "BRENNEN-MENTAL_BOOM_POWER.description": "Whenever you lose HP during your turn, draw {Amount} card(s).",
        "BRENNEN-FOR_THE_TEAM_POWER.title": "For the Team",
        "BRENNEN-FOR_THE_TEAM_POWER.description": "Whenever you lose HP during your turn, gain {Amount} [gold]Vigor[/gold].",
        "BRENNEN-PENTA_SECURE_POWER.title": "Penta Secure",
        "BRENNEN-PENTA_SECURE_POWER.description": "Whenever you play your 5th Attack in a single turn, that Attack triggers twice.",
    }
    write(LOC_POWERS, json.dumps(powers, indent=2, ensure_ascii=False))

    relics = json.loads(LOC_RELICS.read_text(encoding="utf-8"))
    relics["BRENNEN-DUO_QUEUE.title"] = "Duo Queue"
    relics["BRENNEN-DUO_QUEUE.description"] = "Whenever you kill an enemy, gain [blue]1[/blue] [gold]Fed[/gold]."
    relics["BRENNEN-DUO_QUEUE.flavor"] = "I've got you. Don't feed."
    write(LOC_RELICS, json.dumps(relics, indent=2, ensure_ascii=False))


def main() -> None:
    print("=== Brennen redesign emit ===")
    emit_systems()
    emit_basic()
    emit_commons()
    emit_uncommons()
    emit_rares()
    cleanup()
    emit_brennen_character()
    emit_loc()
    print("=== done ===")
    print("cards keep:", sorted(KEEP_CARDS))


if __name__ == "__main__":
    main()
