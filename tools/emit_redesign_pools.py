#!/usr/bin/env python3
"""Emit curated Brennen + Whitney redesign card pools (~35 each)."""
from __future__ import annotations

import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BRENNEN = ROOT / "mods" / "brennen"
WHITNEY = ROOT / "mods" / "whitney"
B_CARDS = BRENNEN / "BrennenCode" / "Cards"
W_CARDS = WHITNEY / "WhitneyCode" / "Cards"
B_LOC = BRENNEN / "Brennen" / "localization" / "eng" / "cards.json"
W_LOC = WHITNEY / "Whitney" / "localization" / "eng" / "cards.json"
B_POW = BRENNEN / "BrennenCode" / "Powers"
W_POW = WHITNEY / "WhitneyCode" / "Powers"

# ---------------------------------------------------------------------------
# Shared emit helpers
# ---------------------------------------------------------------------------

def loc_key(name: str, prefix: str) -> str:
    snake = []
    for i, ch in enumerate(name):
        if ch.isupper() and i:
            snake.append("_")
        snake.append(ch.upper())
    return f"{prefix}-{''.join(snake)}"


def write_cs(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content.strip() + "\n", encoding="utf-8")


def clear_generated_cards(cards_root: Path, keep: set[str]) -> None:
    for rarity in ("Basic", "Common", "Uncommon", "Rare"):
        folder = cards_root / rarity
        if not folder.is_dir():
            continue
        for p in folder.glob("*.cs"):
            if p.stem not in keep:
                p.unlink()
                print("removed", p.relative_to(ROOT))


# ---------------------------------------------------------------------------
# BRENNEN systems + cards
# ---------------------------------------------------------------------------

BRENNEN_KEEP = {
    "BrennenCard",
}

def emit_brennen_systems() -> None:
    write_cs(BRENNEN / "BrennenCode" / "Tilted.cs", r'''
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
''')

    write_cs(B_POW / "FedPower.cs", r'''
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
            "Your snowball. Never decreases during combat. Scales kill payoffs.",
            "You have {Amount} [gold]Fed[/gold].");
}
''')

    write_cs(BRENNEN / "BrennenCode" / "Fed.cs", r'''
using System.Threading.Tasks;
using Brennen.BrennenCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Brennen.BrennenCode;

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

        // Snowball package: on Fed gain → Block + draw
        var snow = owner.Creature.GetPower<SnowballPower>();
        if (snow is not null && snow.Amount > 0)
        {
            snow.Flash();
            await CreatureCmd.GainBlock(
                owner.Creature, snow.Amount, MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered, null);
            await CardPileCmd.Draw(choiceContext, 1, owner);
        }
    }
}
''')

    # Snowball rewritten to trigger on Fed gain (handled in Fed.Gain) — keep AfterDamageGiven empty or remove kill trigger
    write_cs(B_POW / "SnowballPower.cs", r'''
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
''')

    write_cs(B_POW / "BountyPower.cs", r'''
using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Brennen.BrennenCode.Powers;

/// <summary>Fed gains are doubled while Tilted.</summary>
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
''')

    write_cs(B_POW / "MainCharacterPower.cs", r'''
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
''')

    write_cs(B_POW / "GuardianAngelPower.cs", r'''
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Brennen.BrennenCode.Powers;

/// <summary>First lethal hit: heal to 25% HP instead, then remove.</summary>
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
        // Returning false prevents death for this check; we heal in AfterPreventingDeath.
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
''')

    write_cs(B_POW / "PentaSecurePower.cs", r'''
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Brennen.BrennenCode.Powers;

/// <summary>5th Attack each turn triggers twice (Amount unused; Amount=1 means also Fed on trigger when upgraded via card).</summary>
public sealed class PentaSecurePower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private int _attacksThisTurn;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Penta Secure",
            "Whenever you play your 5th Attack in a single turn, that Attack triggers twice.",
            "Whenever you play your 5th Attack in a single turn, that Attack triggers twice.");

    public override async Task AfterSideTurnStart(
        MegaCrit.Sts2.Core.Combat.CombatSide side,
        IReadOnlyList<MegaCrit.Sts2.Core.Entities.Creatures.Creature> participants,
        MegaCrit.Sts2.Core.Combat.ICombatState combatState)
    {
        if (participants.Contains(Owner))
            _attacksThisTurn = 0;
        await Task.CompletedTask;
    }

    // Hook used by cards: Call CheckAndDouble via card play tracking in BrennenCombatHooks if needed.
    // Implemented via AfterCardPlayed on the power if available.
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
            return;
        if (cardPlay.Card.Type != CardType.Attack)
            return;
        _attacksThisTurn++;
        await Task.CompletedTask;
    }

    public bool IsFifthAttackWindow => _attacksThisTurn == 5;
}
''')

    write_cs(B_POW / "MentalBoomPower.cs", r'''
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
''')

    write_cs(B_POW / "ForTheTeamPower.cs", r'''
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
''')

    write_cs(BRENNEN / "BrennenCode" / "Relics" / "DuoQueue.cs", r'''
using System.Collections.Generic;
using System.Threading.Tasks;
using Brennen.BrennenCode;
using Brennen.BrennenCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Relics;

/// <summary>Starter: whenever an enemy dies, gain 1 Fed.</summary>
public sealed class DuoQueue : BrennenRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

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
        if (target.Side == Owner.Creature.Side)
            return;
        Flash();
        await Fed.Gain(choiceContext, Owner, 1);
    }
}
''')


# Card templates as (folder, name, code_body without usings)
# We'll emit full files from a compact data table + template functions.

def brennen_attack(name: str, rarity: str, cost: int, target: str, dmg: int, up_dmg: int,
                   extra_vars="", extra_tips="", on_play_extra="", keywords="", gains_block=False,
                   title=None, desc=None) -> tuple:
    kw = f"\n    public override IEnumerable<CardKeyword> CanonicalKeywords => [{keywords}];\n" if keywords else ""
    gb = "\n    public override bool GainsBlock => true;\n" if gains_block else ""
    tips = f"\n    protected override IEnumerable<IHoverTip> ExtraHoverTips =>\n    [\n{extra_tips}\n    ];\n" if extra_tips else ""
    return name, rarity, f'''
using BaseLib.Utils;
using Brennen.BrennenCode;
using Brennen.BrennenCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Cards.{rarity};

public sealed class {name}() : BrennenCard({cost}, CardType.Attack, CardRarity.{rarity}, TargetType.{target})
{{{kw}{gb}{tips}
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar({dmg}, ValueProp.Move),{extra_vars}
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {{
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
{on_play_extra}
    }}

    protected override void OnUpgrade()
    {{
        DynamicVars.Damage.UpgradeValueBy({up_dmg}m);
    }}
}}
''', title or name, desc


print("scaffolding brennen systems...")
emit_brennen_systems()
print("brennen systems ok")
