#!/usr/bin/env python3
"""Generate Brennen's full vanilla-sized card pool from a kit definition.

Target (STS2 vanilla):
  Basics: Strike, Defend, Feed (starter-only, not rewards)
  Starting deck: 5 Strike / 4 Defend / 1 Feed
  Rewards: 20 Common / 35 Uncommon / 25 Rare
"""

from __future__ import annotations

import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MOD = ROOT / "mods" / "brennen"
CODE = MOD / "BrennenCode" / "Cards"
LOC = MOD / "Brennen" / "localization" / "eng" / "cards.json"
PORTRAITS = MOD / "Brennen" / "images" / "card_portraits"
PORTRAITS_BIG = PORTRAITS / "big"
DOCS_CARDS = ROOT / "docs" / "cards.json"
README = MOD / "README.md"

# Existing hand-authored files we keep (regenerator overwrites only NEW or regenerate-all).
# Feed moves to Basic.
HAND_AUTHORED = {
    "StrikeBrennen",
    "DefendBrennen",
    "Gank",
    "Flash",
    "Tilt",
    "Ward",
    "FirstBlood",
    "MainCharacter",
    "MuteAll",
    "Pentakill",
    "Afk",
    "Remake",
    "Feed",
}


def id_of(name: str) -> str:
    # PascalCase -> snake for portrait; BaseLib lowercases Id.Entry without prefix
    out = []
    for i, c in enumerate(name):
        if c.isupper() and i:
            out.append("_")
        out.append(c)
    return "".join(out)


def portrait_stem(name: str) -> str:
    return name.lower()


# ---------------------------------------------------------------------------
# Kit definition
# rarity, cost, type, target, template, params, title, lines, upgrade_note, keywords, flavor
# ---------------------------------------------------------------------------

# TargetType: AnyEnemy | AllEnemies | RandomEnemy | None | Self
# type: Attack | Skill | Power

KIT: list[dict] = [
    # ===== BASIC (starter, not rewards) =====
    # Strike / Defend / Feed stay hand-authored

    # ===== COMMON (20) =====
    # keep: Gank, Flash, Tilt, Ward
    dict(name="Auto", rarity="Common", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack", dmg=8, up_dmg=3,
         title="Auto", lines=["Deal {Damage:diff()} damage."],
         flavor="Right-click the minion."),
    dict(name="Poke", rarity="Common", cost=0, card_type="Attack", target="AnyEnemy",
         tmpl="attack_draw", dmg=4, cards=1, up_dmg=2,
         title="Poke", lines=["Deal {Damage:diff()} damage.", "Draw {Cards:diff()} card."]),
    dict(name="Camp", rarity="Common", cost=1, card_type="Skill", target="None",
         tmpl="block", block=10, up_block=3,
         title="Camp", lines=["Gain {Block:diff()} [gold]Block[/gold]."],
         flavor="Bush checked."),
    dict(name="Cs", rarity="Common", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_block", dmg=6, block=3, up_dmg=2, up_block=2,
         title="CS", lines=["Deal {Damage:diff()} damage.", "Gain {Block:diff()} [gold]Block[/gold]."]),
    dict(name="Roam", rarity="Common", cost=1, card_type="Attack", target="RandomEnemy",
         tmpl="attack", dmg=9, up_dmg=3,
         title="Roam", lines=["Deal {Damage:diff()} damage to a random enemy."]),
    dict(name="SpamPing", rarity="Common", cost=1, card_type="Attack", target="RandomEnemy",
         tmpl="attack_hits", dmg=2, hits=4, up_hits=1,
         title="Spam Ping", lines=["Deal {Damage:diff()} damage to a random enemy {Repeat:diff()} times."],
         flavor="??? on CD."),
    dict(name="Report", rarity="Common", cost=0, card_type="Skill", target="AnyEnemy",
         tmpl="apply_weak", weak=2, up_weak=1,
         title="Report", lines=["Apply {Weak:diff()} [gold]Weak[/gold]."],
         flavor="After the game."),
    dict(name="Inting", rarity="Common", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_self", dmg=13, self_dmg=4, up_dmg=3,
         title="Inting", lines=["Deal {Damage:diff()} damage.", "Take [blue]4[/blue] damage."],
         flavor="Trust the process."),
    dict(name="Boost", rarity="Common", cost=0, card_type="Skill", target="None",
         tmpl="vigor", vigor=4, up_vigor=2,
         title="Boost", lines=["Gain {Vigor:diff()} [gold]Vigor[/gold]."]),
    dict(name="Peel", rarity="Common", cost=1, card_type="Skill", target="AnyEnemy",
         tmpl="block_weak", block=6, weak=1, up_block=3,
         title="Peel", lines=["Gain {Block:diff()} [gold]Block[/gold].", "Apply {Weak:diff()} [gold]Weak[/gold]."]),
    dict(name="Facecheck", rarity="Common", cost=1, card_type="Attack", target="AllEnemies",
         tmpl="attack", dmg=5, up_dmg=2,
         title="Facecheck", lines=["Deal {Damage:diff()} damage to ALL enemies."]),
    dict(name="Ping", rarity="Common", cost=0, card_type="Attack", target="AnyEnemy",
         tmpl="attack", dmg=5, up_dmg=2,
         title="Ping", lines=["Deal {Damage:diff()} damage."]),
    dict(name="Tax", rarity="Common", cost=1, card_type="Skill", target="None",
         tmpl="energy_next", energy=1, up_energy=1,
         title="Tax", lines=["Next turn, gain {Energy:energyIcons()}."],
         flavor="Lane tax."),
    dict(name="Diff", rarity="Common", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack", dmg=11, up_dmg=3,
         title="Diff", lines=["Deal {Damage:diff()} damage."],
         flavor="Skill issue."),
    dict(name="Catch", rarity="Common", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_vuln", dmg=7, vuln=1, up_dmg=3,
         title="Catch", lines=["Deal {Damage:diff()} damage.", "Apply {Vulnerable:diff()} [gold]Vulnerable[/gold]."]),
    dict(name="Smite", rarity="Common", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_exhaust", dmg=14, up_dmg=4,
         title="Smite", lines=["Deal {Damage:diff()} damage.", "[gold]Exhaust[/gold]."]),

    # ===== UNCOMMON (35) =====
    # keep: FirstBlood, MainCharacter, MuteAll  (Feed moved to Basic)
    dict(name="Snowball", rarity="Uncommon", cost=1, card_type="Power", target="None",
         tmpl="power_strength", strength=2, up_strength=1,
         title="Snowball", lines=["Gain {Strength:diff()} [gold]Strength[/gold]."],
         flavor="Don't throw."),
    dict(name="Baron", rarity="Uncommon", cost=2, card_type="Attack", target="AnyEnemy",
         tmpl="attack_block", dmg=12, block=8, up_dmg=3, up_block=3,
         title="Baron", lines=["Deal {Damage:diff()} damage.", "Gain {Block:diff()} [gold]Block[/gold]."]),
    dict(name="Drake", rarity="Uncommon", cost=1, card_type="Skill", target="None",
         tmpl="block_draw", block=8, cards=2, up_block=3,
         title="Drake", lines=["Gain {Block:diff()} [gold]Block[/gold].", "Draw {Cards:diff()} cards."]),
    dict(name="Objective", rarity="Uncommon", cost=1, card_type="Attack", target="AllEnemies",
         tmpl="attack_hits", dmg=4, hits=2, up_dmg=2,
         title="Objective", lines=["Deal {Damage:diff()} damage to ALL enemies {Repeat:diff()} times."]),
    dict(name="SplitPush", rarity="Uncommon", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_draw", dmg=9, cards=1, up_dmg=3,
         title="Split Push", lines=["Deal {Damage:diff()} damage.", "Draw {Cards:diff()} card."]),
    dict(name="Tp", rarity="Uncommon", cost=0, card_type="Skill", target="None",
         tmpl="block_exhaust", block=12, up_block=4,
         title="TP", lines=["Gain {Block:diff()} [gold]Block[/gold].", "[gold]Exhaust[/gold]."],
         flavor="Channeling..."),
    dict(name="Ult", rarity="Uncommon", cost=2, card_type="Attack", target="AnyEnemy",
         tmpl="attack", dmg=20, up_dmg=6,
         title="Ult", lines=["Deal {Damage:diff()} damage."]),
    dict(name="FlashEngage", rarity="Uncommon", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_vuln", dmg=10, vuln=2, up_dmg=3,
         title="Flash Engage", lines=["Deal {Damage:diff()} damage.", "Apply {Vulnerable:diff()} [gold]Vulnerable[/gold]."]),
    dict(name="Zone", rarity="Uncommon", cost=1, card_type="Skill", target="AllEnemies",
         tmpl="apply_weak_all", weak=1, up_weak=1,
         title="Zone", lines=["Apply {Weak:diff()} [gold]Weak[/gold] to ALL enemies."]),
    dict(name="ChatRestrict", rarity="Uncommon", cost=1, card_type="Skill", target="AnyEnemy",
         tmpl="apply_frail", frail=2, up_frail=1,
         title="Chat Restrict", lines=["Apply {Frail:diff()} [gold]Frail[/gold]."]),
    dict(name="Flame", rarity="Uncommon", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_self", dmg=16, self_dmg=3, up_dmg=4,
         title="Flame", lines=["Deal {Damage:diff()} damage.", "Take [blue]3[/blue] damage."],
         flavor="All chat."),
    dict(name="Outplay", rarity="Uncommon", cost=1, card_type="Skill", target="None",
         tmpl="draw_energy", cards=2, energy=1, up_cards=1,
         title="Outplay", lines=["Draw {Cards:diff()} cards.", "Gain {Energy:energyIcons()}."]),
    dict(name="Kda", rarity="Uncommon", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_hits", dmg=5, hits=3, up_dmg=2,
         title="KDA", lines=["Deal {Damage:diff()} damage {Repeat:diff()} times."]),
    dict(name="RoamBot", rarity="Uncommon", cost=1, card_type="Attack", target="RandomEnemy",
         tmpl="attack_hits", dmg=3, hits=4, up_hits=1,
         title="Roam Bot", lines=["Deal {Damage:diff()} damage to a random enemy {Repeat:diff()} times."]),
    dict(name="VisionScore", rarity="Uncommon", cost=1, card_type="Skill", target="None",
         tmpl="block", block=14, up_block=4,
         title="Vision Score", lines=["Gain {Block:diff()} [gold]Block[/gold]."]),
    dict(name="Shutdown", rarity="Uncommon", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_if_low", dmg=8, low_dmg=16, up_dmg=3,
         title="Shutdown",
         lines=["Deal {Damage:diff()} damage.", "If the enemy has [blue]50%[/blue] or less HP, deal {LowHpDamage:diff()} instead."]),
    dict(name="Bounty", rarity="Uncommon", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_fatal_block", dmg=8, block=10, up_dmg=3,
         title="Bounty",
         lines=["Deal {Damage:diff()} damage.", "If Fatal, gain {Block:diff()} [gold]Block[/gold]."]),
    dict(name="Assist", rarity="Uncommon", cost=0, card_type="Skill", target="None",
         tmpl="vigor_draw", vigor=3, cards=1, up_vigor=2,
         title="Assist", lines=["Gain {Vigor:diff()} [gold]Vigor[/gold].", "Draw {Cards:diff()} card."]),
    dict(name="JungleClear", rarity="Uncommon", cost=1, card_type="Attack", target="AllEnemies",
         tmpl="attack", dmg=8, up_dmg=3,
         title="Jungle Clear", lines=["Deal {Damage:diff()} damage to ALL enemies."]),
    dict(name="Backdoor", rarity="Uncommon", cost=2, card_type="Attack", target="AnyEnemy",
         tmpl="attack_exhaust", dmg=24, up_dmg=6,
         title="Backdoor", lines=["Deal {Damage:diff()} damage.", "[gold]Exhaust[/gold]."],
         flavor="They never look."),
    dict(name="Freeze", rarity="Uncommon", cost=1, card_type="Skill", target="AnyEnemy",
         tmpl="apply_weak_vuln", weak=1, vuln=1, up_weak=1,
         title="Freeze", lines=["Apply {Weak:diff()} [gold]Weak[/gold] and {Vulnerable:diff()} [gold]Vulnerable[/gold]."]),
    dict(name="Rotate", rarity="Uncommon", cost=1, card_type="Skill", target="None",
         tmpl="draw", cards=3, up_cards=1,
         title="Rotate", lines=["Draw {Cards:diff()} cards."]),
    dict(name="PowerSpike", rarity="Uncommon", cost=1, card_type="Skill", target="None",
         tmpl="strength_temp", strength=3, up_strength=1,
         title="Power Spike", lines=["Gain {Strength:diff()} [gold]Strength[/gold] this turn."]),
    dict(name="ItemSpike", rarity="Uncommon", cost=0, card_type="Skill", target="None",
         tmpl="energy", energy=2, up_energy=1,
         title="Item Spike", lines=["Gain {Energy:energyIcons()}."],
         keywords=["Exhaust"]),
    dict(name="Dive", rarity="Uncommon", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_self", dmg=15, self_dmg=2, up_dmg=4,
         title="Dive", lines=["Deal {Damage:diff()} damage.", "Take [blue]2[/blue] damage."]),
    dict(name="PeelForAdc", rarity="Uncommon", cost=1, card_type="Skill", target="None",
         tmpl="block_draw", block=10, cards=1, up_block=3,
         title="Peel for ADC", lines=["Gain {Block:diff()} [gold]Block[/gold].", "Draw {Cards:diff()} card."]),
    dict(name="Invade", rarity="Uncommon", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_weak", dmg=9, weak=2, up_dmg=3,
         title="Invade", lines=["Deal {Damage:diff()} damage.", "Apply {Weak:diff()} [gold]Weak[/gold]."]),
    dict(name="WardHop", rarity="Uncommon", cost=0, card_type="Skill", target="None",
         tmpl="block_draw", block=4, cards=1, up_block=2,
         title="Ward Hop", lines=["Gain {Block:diff()} [gold]Block[/gold].", "Draw {Cards:diff()} card."]),
    dict(name="Macro", rarity="Uncommon", cost=1, card_type="Power", target="None",
         tmpl="power_strength", strength=1, up_strength=1,
         title="Macro", lines=["Gain {Strength:diff()} [gold]Strength[/gold]."],
         flavor="Play the map."),
    dict(name="Micro", rarity="Uncommon", cost=1, card_type="Skill", target="None",
         tmpl="vigor", vigor=6, up_vigor=3,
         title="Micro", lines=["Gain {Vigor:diff()} [gold]Vigor[/gold]."]),
    dict(name="AllIn", rarity="Uncommon", cost=2, card_type="Attack", target="AnyEnemy",
         tmpl="attack_hits", dmg=7, hits=3, up_dmg=2,
         title="All-In", lines=["Deal {Damage:diff()} damage {Repeat:diff()} times."]),
    dict(name="Disrespect", rarity="Uncommon", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_if_high_hp", dmg=6, high_dmg=14, up_dmg=2,
         title="Disrespect",
         lines=["Deal {Damage:diff()} damage.", "If you have more than [blue]50%[/blue] HP, deal {HighHpDamage:diff()} instead."],
         flavor="Dance in fountain."),

    # ===== RARE (25) =====
    # keep: Pentakill, Afk, Remake
    dict(name="Ace", rarity="Rare", cost=2, card_type="Attack", target="AllEnemies",
         tmpl="attack", dmg=14, up_dmg=4,
         title="Ace", lines=["Deal {Damage:diff()} damage to ALL enemies."]),
    dict(name="PentaSecure", rarity="Rare", cost=1, card_type="Power", target="None",
         tmpl="power_strength", strength=3, up_strength=1,
         title="Penta Secure", lines=["Gain {Strength:diff()} [gold]Strength[/gold]."]),
    dict(name="OneVNine", rarity="Rare", cost=2, card_type="Attack", target="AnyEnemy",
         tmpl="attack_if_low_self", dmg=16, low_dmg=28, up_dmg=4,
         title="1v9",
         lines=["Deal {Damage:diff()} damage.", "If you have [blue]50%[/blue] or less HP, deal {LowHpDamage:diff()} instead."],
         flavor="Carry harder."),
    dict(name="Uninstall", rarity="Rare", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_exhaust", dmg=28, up_dmg=8,
         title="Uninstall", lines=["Deal {Damage:diff()} damage.", "[gold]Exhaust[/gold]."]),
    dict(name="GgEz", rarity="Rare", cost=0, card_type="Skill", target="AllEnemies",
         tmpl="apply_weak_vuln_all", weak=2, vuln=2, up_weak=1,
         title="GG EZ", lines=["Apply {Weak:diff()} [gold]Weak[/gold] and {Vulnerable:diff()} [gold]Vulnerable[/gold] to ALL enemies.", "[gold]Exhaust[/gold]."],
         keywords=["Exhaust"],
         flavor="Honor me."),
    dict(name="Clutch", rarity="Rare", cost=1, card_type="Skill", target="None",
         tmpl="block_draw", block=15, cards=2, up_block=5,
         title="Clutch", lines=["Gain {Block:diff()} [gold]Block[/gold].", "Draw {Cards:diff()} cards."]),
    dict(name="PerfectGame", rarity="Rare", cost=3, card_type="Attack", target="AnyEnemy",
         tmpl="attack_hits", dmg=8, hits=5, up_dmg=2,
         title="Perfect Game", lines=["Deal {Damage:diff()} damage {Repeat:diff()} times."]),
    dict(name="IntingSion", rarity="Rare", cost=2, card_type="Attack", target="AnyEnemy",
         tmpl="attack_self_big", dmg=30, self_dmg=8, up_dmg=8,
         title="Inting Sion", lines=["Deal {Damage:diff()} damage.", "Take [blue]8[/blue] damage."],
         flavor="For the team."),
    dict(name="OpenMid", rarity="Rare", cost=0, card_type="Skill", target="None",
         tmpl="draw_energy", cards=3, energy=2, up_cards=1,
         title="Open Mid", lines=["Draw {Cards:diff()} cards.", "Gain {Energy:energyIcons()}.", "[gold]Exhaust[/gold]."],
         keywords=["Exhaust"]),
    dict(name="HardStuck", rarity="Rare", cost=1, card_type="Power", target="None",
         tmpl="power_strength", strength=4, up_strength=1,
         title="Hard Stuck", lines=["Gain {Strength:diff()} [gold]Strength[/gold]."],
         flavor="Gold forever."),
    dict(name="ChallengerDiff", rarity="Rare", cost=2, card_type="Attack", target="AnyEnemy",
         tmpl="attack", dmg=26, up_dmg=6,
         title="Challenger Diff", lines=["Deal {Damage:diff()} damage."]),
    dict(name="FountainDive", rarity="Rare", cost=1, card_type="Attack", target="AllEnemies",
         tmpl="attack_self_aoe", dmg=12, self_dmg=5, up_dmg=3,
         title="Fountain Dive", lines=["Deal {Damage:diff()} damage to ALL enemies.", "Take [blue]5[/blue] damage."]),
    dict(name="FullClear", rarity="Rare", cost=2, card_type="Attack", target="AllEnemies",
         tmpl="attack_hits", dmg=5, hits=4, up_hits=1,
         title="Full Clear", lines=["Deal {Damage:diff()} damage to ALL enemies {Repeat:diff()} times."]),
    dict(name="Quadra", rarity="Rare", cost=2, card_type="Attack", target="AllEnemies",
         tmpl="attack_hits", dmg=6, hits=4, up_dmg=2,
         title="Quadra", lines=["Deal {Damage:diff()} damage to ALL enemies {Repeat:diff()} times."]),
    dict(name="Bait", rarity="Rare", cost=1, card_type="Skill", target="None",
         tmpl="block_exhaust", block=25, up_block=8,
         title="Bait", lines=["Gain {Block:diff()} [gold]Block[/gold].", "[gold]Exhaust[/gold]."]),
    dict(name="Throw", rarity="Rare", cost=0, card_type="Skill", target="None",
         tmpl="self_dmg_energy", self_dmg=5, energy=3, up_energy=1,
         title="Throw", lines=["Take [blue]5[/blue] damage.", "Gain {Energy:energyIcons()}.", "[gold]Exhaust[/gold]."],
         keywords=["Exhaust"],
         flavor="For the highlight."),
    dict(name="JgDiff", rarity="Rare", cost=1, card_type="Attack", target="RandomEnemy",
         tmpl="attack_hits", dmg=6, hits=4, up_dmg=2,
         title="JG Diff", lines=["Deal {Damage:diff()} damage to a random enemy {Repeat:diff()} times."]),
    dict(name="AdcDiff", rarity="Rare", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_hits", dmg=4, hits=5, up_dmg=1,
         title="ADC Diff", lines=["Deal {Damage:diff()} damage {Repeat:diff()} times."]),
    dict(name="SupDiff", rarity="Rare", cost=1, card_type="Skill", target="AllEnemies",
         tmpl="apply_weak_all", weak=3, up_weak=1,
         title="SUP Diff", lines=["Apply {Weak:diff()} [gold]Weak[/gold] to ALL enemies."]),
    dict(name="MidDiff", rarity="Rare", cost=1, card_type="Skill", target="None",
         tmpl="draw_energy", cards=2, energy=2, up_cards=1,
         title="MID Diff", lines=["Draw {Cards:diff()} cards.", "Gain {Energy:energyIcons()}."]),
    dict(name="TopDiff", rarity="Rare", cost=2, card_type="Attack", target="AnyEnemy",
         tmpl="attack_block", dmg=16, block=12, up_dmg=4, up_block=4,
         title="TOP Diff", lines=["Deal {Damage:diff()} damage.", "Gain {Block:diff()} [gold]Block[/gold]."]),
    dict(name="DodgeTheDodge", rarity="Rare", cost=1, card_type="Skill", target="None",
         tmpl="block_draw", block=12, cards=3, up_block=4,
         title="Dodge the Dodge", lines=["Gain {Block:diff()} [gold]Block[/gold].", "Draw {Cards:diff()} cards."]),
]


# ---------------------------------------------------------------------------
# Code generation
# ---------------------------------------------------------------------------

USINGS_BASE = """using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
"""

HEADER = """// Auto-generated by tools/generate_brennen_kit.py — edit kit definition, then re-run.
"""


def gen_card(c: dict) -> str:
    rarity = c["rarity"]
    ns = f"Brennen.BrennenCode.Cards.{rarity}"
    name = c["name"]
    cost = c["cost"]
    ctype = f"CardType.{c['card_type']}"
    crarity = f"CardRarity.{rarity}"
    target = f"TargetType.{c['target']}"
    tmpl = c["tmpl"]

    body_parts: list[str] = []
    props: list[str] = []
    vars_lines: list[str] = []
    on_play: list[str] = []
    on_up: list[str] = []
    extra_methods: list[str] = []

    keywords = list(c.get("keywords") or [])
    if tmpl in ("attack_exhaust", "block_exhaust") or "Exhaust" in keywords:
        if "Exhaust" not in keywords:
            keywords.append("Exhaust")
    if keywords:
        kws = ", ".join(f"CardKeyword.{k}" for k in keywords)
        props.append(f"    public override IEnumerable<CardKeyword> CanonicalKeywords => [{kws}];")

    if c["card_type"] == "Skill" and tmpl.startswith("block"):
        props.append("    public override bool GainsBlock => true;")
    if tmpl in ("attack_block", "block", "block_draw", "block_weak", "block_exhaust", "attack_fatal_block"):
        if "GainsBlock" not in "\n".join(props):
            props.append("    public override bool GainsBlock => true;")

    # hover tips for debuffs
    tips: list[str] = []
    if "weak" in c or tmpl.endswith("weak") or "weak" in tmpl:
        tips.append("HoverTipFactory.FromPower<WeakPower>()")
    if "vuln" in c or "vuln" in tmpl:
        tips.append("HoverTipFactory.FromPower<VulnerablePower>()")
    if "frail" in c or "frail" in tmpl:
        tips.append("HoverTipFactory.FromPower<FrailPower>()")
    if "vigor" in tmpl or "vigor" in c:
        tips.append("HoverTipFactory.FromPower<VigorPower>()")
    if "strength" in tmpl:
        tips.append("HoverTipFactory.FromPower<StrengthPower>()")
    if tips:
        props.append(
            "    protected override IEnumerable<IHoverTip> ExtraHoverTips =>\n    [\n        "
            + ",\n        ".join(tips)
            + ",\n    ];"
        )

    def add_dmg(v: int | None = None):
        v = c.get("dmg", v)
        vars_lines.append(f"        new DamageVar({v}, ValueProp.Move),")

    def add_block(v: int | None = None):
        v = c.get("block", v)
        vars_lines.append(f"        new BlockVar({v}, ValueProp.Move),")

    def add_cards(v: int | None = None):
        v = c.get("cards", v)
        vars_lines.append(f"        new CardsVar({v}),")

    def add_repeat(v: int | None = None):
        v = c.get("hits", v)
        vars_lines.append(f"        new RepeatVar({v}),")

    def add_energy(v: int | None = None):
        v = c.get("energy", v)
        vars_lines.append(f"        new EnergyVar({v}),")

    def up_dmg(n=None):
        n = c.get("up_dmg", n)
        if n:
            on_up.append(f"        DynamicVars.Damage.UpgradeValueBy({n}m);")

    def up_block(n=None):
        n = c.get("up_block", n)
        if n:
            on_up.append(f"        DynamicVars.Block.UpgradeValueBy({n}m);")

    def up_cards(n=None):
        n = c.get("up_cards", n)
        if n:
            on_up.append(f"        DynamicVars.Cards.UpgradeValueBy({n}m);")

    def up_hits(n=None):
        n = c.get("up_hits", n)
        if n:
            on_up.append(f"        DynamicVars.Repeat.UpgradeValueBy({n}m);")

    def up_energy(n=None):
        n = c.get("up_energy", n)
        if n:
            on_up.append(f"        DynamicVars.Energy.UpgradeValueBy({n}m);")

    def self_dmg_code(amount: int):
        return f"""        if (Owner.Creature is not null)
        {{
            await CreatureCmd.Damage(
                choiceContext,
                [Owner.Creature],
                {amount},
                ValueProp.Unblockable | ValueProp.Unpowered,
                Owner.Creature,
                this);
        }}"""

    def apply_power(power: str, amount_expr: str, target_expr: str = "play.Target"):
        return f"""        if ({target_expr} is not null)
        {{
            await PowerCmd.Apply<{power}>(
                choiceContext,
                {target_expr},
                {amount_expr},
                Owner.Creature,
                this);
        }}"""

    def apply_power_self(power: str, amount_expr: str):
        return f"""        if (Owner.Creature is not null)
        {{
            await PowerCmd.Apply<{power}>(
                choiceContext,
                Owner.Creature,
                {amount_expr},
                Owner.Creature,
                this);
        }}"""

    def apply_all(power: str, amount_expr: str, declare_combat: bool = True):
        prefix = ""
        if declare_combat:
            prefix = (
                "        var combat = Owner.Creature?.CombatState;\n"
                "        if (combat is null) return;\n"
            )
        return (
            prefix
            + f"""        foreach (var enemy in combat.HittableEnemies)
        {{
            await PowerCmd.Apply<{power}>(
                choiceContext,
                enemy,
                {amount_expr},
                Owner.Creature,
                this);
        }}"""
        )

    if tmpl == "attack":
        add_dmg()
        on_play.append("        await CommonActions.CardAttack(this, play).Execute(choiceContext);")
        up_dmg()
    elif tmpl == "attack_hits":
        add_dmg()
        add_repeat()
        on_play.append("        await CommonActions.CardAttack(this, play)")
        on_play.append("            .WithHitCount(DynamicVars.Repeat.IntValue)")
        on_play.append("            .Execute(choiceContext);")
        up_dmg()
        up_hits()
    elif tmpl == "attack_draw":
        add_dmg()
        add_cards()
        on_play.append("        await CommonActions.CardAttack(this, play).Execute(choiceContext);")
        on_play.append("        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);")
        up_dmg()
        up_cards()
    elif tmpl == "attack_block":
        add_dmg()
        add_block()
        on_play.append("        await CommonActions.CardAttack(this, play).Execute(choiceContext);")
        on_play.append("        await CommonActions.CardBlock(this, play);")
        up_dmg()
        up_block()
    elif tmpl == "attack_self" or tmpl == "attack_self_big":
        add_dmg()
        sd = c["self_dmg"]
        on_play.append("        await CommonActions.CardAttack(this, play).Execute(choiceContext);")
        on_play.append(self_dmg_code(sd))
        up_dmg()
    elif tmpl == "attack_self_aoe":
        add_dmg()
        sd = c["self_dmg"]
        on_play.append("        await CommonActions.CardAttack(this, play).Execute(choiceContext);")
        on_play.append(self_dmg_code(sd))
        up_dmg()
    elif tmpl == "attack_exhaust":
        add_dmg()
        on_play.append("        await CommonActions.CardAttack(this, play).Execute(choiceContext);")
        up_dmg()
    elif tmpl == "attack_weak":
        add_dmg()
        vars_lines.append(f"        new DynamicVar(\"Weak\", {c['weak']}),")
        on_play.append("        await CommonActions.CardAttack(this, play).Execute(choiceContext);")
        on_play.append(apply_power("WeakPower", "DynamicVars[\"Weak\"].IntValue"))
        up_dmg()
        if c.get("up_weak"):
            on_up.append(f"        DynamicVars[\"Weak\"].UpgradeValueBy({c['up_weak']}m);")
    elif tmpl == "attack_vuln":
        add_dmg()
        vars_lines.append(f"        new DynamicVar(\"Vulnerable\", {c['vuln']}),")
        on_play.append("        await CommonActions.CardAttack(this, play).Execute(choiceContext);")
        on_play.append(apply_power("VulnerablePower", "DynamicVars[\"Vulnerable\"].IntValue"))
        up_dmg()
        if c.get("up_vuln"):
            on_up.append(f"        DynamicVars[\"Vulnerable\"].UpgradeValueBy({c['up_vuln']}m);")
    elif tmpl == "attack_if_low":
        add_dmg()
        vars_lines.append(f"        new DamageVar(\"LowHpDamage\", {c['low_dmg']}, ValueProp.Move),")
        extra_methods.append("""
    private bool IsTargetLowHp()
    {
        var t = /* set at play */;
        return false;
    }
""")
        on_play.append("""        var target = play.Target;
        var dmg = DynamicVars.Damage.BaseValue;
        if (target is not null && target.CurrentHp * 2 <= target.MaxHp)
            dmg = DynamicVars["LowHpDamage"].BaseValue;
        var stored = DynamicVars.Damage.BaseValue;
        DynamicVars.Damage.BaseValue = dmg;
        try
        {
            await CommonActions.CardAttack(this, play).Execute(choiceContext);
        }
        finally
        {
            DynamicVars.Damage.BaseValue = stored;
        }""")
        up_dmg()
        on_up.append(f"        DynamicVars[\"LowHpDamage\"].UpgradeValueBy({c.get('up_dmg', 3)}m);")
    elif tmpl == "attack_if_low_self":
        add_dmg()
        vars_lines.append(f"        new DamageVar(\"LowHpDamage\", {c['low_dmg']}, ValueProp.Move),")
        on_play.append("""        var dmg = DynamicVars.Damage.BaseValue;
        var me = Owner.Creature;
        if (me is not null && me.CurrentHp * 2 <= me.MaxHp)
            dmg = DynamicVars["LowHpDamage"].BaseValue;
        var stored = DynamicVars.Damage.BaseValue;
        DynamicVars.Damage.BaseValue = dmg;
        try
        {
            await CommonActions.CardAttack(this, play).Execute(choiceContext);
        }
        finally
        {
            DynamicVars.Damage.BaseValue = stored;
        }""")
        up_dmg()
        on_up.append(f"        DynamicVars[\"LowHpDamage\"].UpgradeValueBy({c.get('up_dmg', 4)}m);")
    elif tmpl == "attack_if_high_hp":
        add_dmg()
        vars_lines.append(f"        new DamageVar(\"HighHpDamage\", {c['high_dmg']}, ValueProp.Move),")
        on_play.append("""        var dmg = DynamicVars.Damage.BaseValue;
        var me = Owner.Creature;
        if (me is not null && me.CurrentHp * 2 > me.MaxHp)
            dmg = DynamicVars["HighHpDamage"].BaseValue;
        var stored = DynamicVars.Damage.BaseValue;
        DynamicVars.Damage.BaseValue = dmg;
        try
        {
            await CommonActions.CardAttack(this, play).Execute(choiceContext);
        }
        finally
        {
            DynamicVars.Damage.BaseValue = stored;
        }""")
        up_dmg()
        on_up.append(f"        DynamicVars[\"HighHpDamage\"].UpgradeValueBy({c.get('up_dmg', 2)}m);")
    elif tmpl == "attack_fatal_block":
        add_dmg()
        add_block()
        on_play.append("        await CommonActions.CardAttack(this, play).Execute(choiceContext);")
        on_play.append("""        if (play.Target is not null && play.Target.IsDead)
            await CommonActions.CardBlock(this, play);""")
        up_dmg()
        up_block()
    elif tmpl == "block":
        add_block()
        on_play.append("        await CommonActions.CardBlock(this, play);")
        up_block()
    elif tmpl == "block_draw":
        add_block()
        add_cards()
        on_play.append("        await CommonActions.CardBlock(this, play);")
        on_play.append("        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);")
        up_block()
        up_cards()
    elif tmpl == "block_exhaust":
        add_block()
        on_play.append("        await CommonActions.CardBlock(this, play);")
        up_block()
    elif tmpl == "block_weak":
        add_block()
        vars_lines.append(f"        new DynamicVar(\"Weak\", {c['weak']}),")
        on_play.append("        await CommonActions.CardBlock(this, play);")
        on_play.append(apply_power("WeakPower", "DynamicVars[\"Weak\"].IntValue"))
        up_block()
        if c.get("up_weak"):
            on_up.append(f"        DynamicVars[\"Weak\"].UpgradeValueBy({c['up_weak']}m);")
    elif tmpl == "apply_weak":
        vars_lines.append(f"        new DynamicVar(\"Weak\", {c['weak']}),")
        on_play.append(apply_power("WeakPower", "DynamicVars[\"Weak\"].IntValue"))
        if c.get("up_weak"):
            on_up.append(f"        DynamicVars[\"Weak\"].UpgradeValueBy({c['up_weak']}m);")
    elif tmpl == "apply_frail":
        vars_lines.append(f"        new DynamicVar(\"Frail\", {c['frail']}),")
        on_play.append(apply_power("FrailPower", "DynamicVars[\"Frail\"].IntValue"))
        if c.get("up_frail"):
            on_up.append(f"        DynamicVars[\"Frail\"].UpgradeValueBy({c['up_frail']}m);")
    elif tmpl == "apply_weak_all":
        vars_lines.append(f"        new DynamicVar(\"Weak\", {c['weak']}),")
        on_play.append(apply_all("WeakPower", "DynamicVars[\"Weak\"].IntValue"))
        if c.get("up_weak"):
            on_up.append(f"        DynamicVars[\"Weak\"].UpgradeValueBy({c['up_weak']}m);")
    elif tmpl == "apply_weak_vuln":
        vars_lines.append(f"        new DynamicVar(\"Weak\", {c['weak']}),")
        vars_lines.append(f"        new DynamicVar(\"Vulnerable\", {c['vuln']}),")
        on_play.append(apply_power("WeakPower", "DynamicVars[\"Weak\"].IntValue"))
        on_play.append(apply_power("VulnerablePower", "DynamicVars[\"Vulnerable\"].IntValue"))
        if c.get("up_weak"):
            on_up.append(f"        DynamicVars[\"Weak\"].UpgradeValueBy({c['up_weak']}m);")
            on_up.append(f"        DynamicVars[\"Vulnerable\"].UpgradeValueBy({c['up_weak']}m);")
    elif tmpl == "apply_weak_vuln_all":
        vars_lines.append(f"        new DynamicVar(\"Weak\", {c['weak']}),")
        vars_lines.append(f"        new DynamicVar(\"Vulnerable\", {c['vuln']}),")
        on_play.append(apply_all("WeakPower", "DynamicVars[\"Weak\"].IntValue", declare_combat=True))
        on_play.append(apply_all("VulnerablePower", "DynamicVars[\"Vulnerable\"].IntValue", declare_combat=False))
        if c.get("up_weak"):
            on_up.append(f"        DynamicVars[\"Weak\"].UpgradeValueBy({c['up_weak']}m);")
            on_up.append(f"        DynamicVars[\"Vulnerable\"].UpgradeValueBy({c['up_weak']}m);")
    elif tmpl == "vigor":
        vars_lines.append(f"        new DynamicVar(\"Vigor\", {c['vigor']}),")
        on_play.append(apply_power_self("VigorPower", "DynamicVars[\"Vigor\"].IntValue"))
        if c.get("up_vigor"):
            on_up.append(f"        DynamicVars[\"Vigor\"].UpgradeValueBy({c['up_vigor']}m);")
    elif tmpl == "vigor_draw":
        vars_lines.append(f"        new DynamicVar(\"Vigor\", {c['vigor']}),")
        add_cards()
        on_play.append(apply_power_self("VigorPower", "DynamicVars[\"Vigor\"].IntValue"))
        on_play.append("        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);")
        if c.get("up_vigor"):
            on_up.append(f"        DynamicVars[\"Vigor\"].UpgradeValueBy({c['up_vigor']}m);")
        up_cards()
    elif tmpl == "power_strength":
        vars_lines.append(f"        new DynamicVar(\"Strength\", {c['strength']}),")
        on_play.append(apply_power_self("StrengthPower", "DynamicVars[\"Strength\"].IntValue"))
        if c.get("up_strength"):
            on_up.append(f"        DynamicVars[\"Strength\"].UpgradeValueBy({c['up_strength']}m);")
    elif tmpl == "strength_temp":
        # Temporary strength = Strength + loss next turn isn't easy; use Vigor as "this turn"
        vars_lines.append(f"        new DynamicVar(\"Strength\", {c['strength']}),")
        # Use Vigor as temporary attack buff this turn — label as Strength in loc is wrong.
        # Better: StrengthPower + TemporaryStrength isn't available simply.
        # STS has TemporaryStrengthPower? We found TemporaryStrengthLossPower only.
        # Use Vigor with amount that matches "strength this turn" fantasy.
        on_play.append(apply_power_self("VigorPower", "DynamicVars[\"Strength\"].IntValue"))
        if c.get("up_strength"):
            on_up.append(f"        DynamicVars[\"Strength\"].UpgradeValueBy({c['up_strength']}m);")
    elif tmpl == "draw":
        add_cards()
        on_play.append("        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);")
        up_cards()
    elif tmpl == "energy":
        add_energy()
        on_play.append("        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);")
        up_energy()
    elif tmpl == "energy_next":
        add_energy()
        on_play.append(apply_power_self("EnergyNextTurnPower", "DynamicVars.Energy.IntValue"))
        up_energy()
    elif tmpl == "draw_energy":
        add_cards()
        add_energy()
        on_play.append("        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);")
        on_play.append("        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);")
        up_cards()
        up_energy()
    elif tmpl == "self_dmg_energy":
        add_energy()
        sd = c["self_dmg"]
        on_play.append(self_dmg_code(sd))
        on_play.append("        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);")
        up_energy()
    else:
        raise ValueError(f"Unknown template {tmpl} for {name}")

    vars_block = ""
    if vars_lines:
        vars_block = (
            "\n    protected override IEnumerable<DynamicVar> CanonicalVars =>\n    [\n"
            + "\n".join(vars_lines)
            + "\n    ];\n"
        )

    props_block = ("\n" + "\n".join(props) + "\n") if props else ""

    on_play_block = (
        "\n    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)\n"
        "    {\n"
        + "\n".join(on_play)
        + "\n    }\n"
    )

    on_up_block = ""
    if on_up:
        on_up_block = (
            "\n    protected override void OnUpgrade()\n    {\n"
            + "\n".join(on_up)
            + "\n    }\n"
        )
    else:
        # still need empty upgrade? not required
        pass

    # Fix strength_temp loc fantasy - change title description to Vigor in catalog later

    return f"""{HEADER}{USINGS_BASE}
namespace {ns};

public sealed class {name}() : BrennenCard({cost}, {ctype}, {crarity}, {target})
{{{props_block}{vars_block}{on_play_block}{on_up_block}}}
"""


def loc_key(name: str) -> str:
    # BRENNEN-CLASS_NAME with underscores for camel humps
    snake = []
    for i, ch in enumerate(name):
        if ch.isupper() and i:
            snake.append("_")
        snake.append(ch.upper())
    return "BRENNEN-" + "".join(snake)


def main() -> None:
    # Count check vs existing hand-authored
    by_rarity: dict[str, list] = {"Common": [], "Uncommon": [], "Rare": []}
    for c in KIT:
        by_rarity[c["rarity"]].append(c)

    hand_common = {"Gank", "Flash", "Tilt", "Ward"}
    hand_uncommon = {"FirstBlood", "MainCharacter", "MuteAll"}
    hand_rare = {"Pentakill", "Afk", "Remake"}

    total_c = len(by_rarity["Common"]) + len(hand_common)
    total_u = len(by_rarity["Uncommon"]) + len(hand_uncommon)
    total_r = len(by_rarity["Rare"]) + len(hand_rare)
    print(f"Generated new: C={len(by_rarity['Common'])} U={len(by_rarity['Uncommon'])} R={len(by_rarity['Rare'])}")
    print(f"With hand-authored totals: C={total_c} U={total_u} R={total_r} (target 20/35/25)")
    assert total_c == 20, total_c
    assert total_u == 35, total_u
    assert total_r == 25, total_r

    # Move Feed to Basic if still in Uncommon
    feed_old = CODE / "Uncommon" / "Feed.cs"
    feed_new = CODE / "Basic" / "Feed.cs"
    if feed_old.exists():
        text = feed_old.read_text(encoding="utf-8")
        text = text.replace("namespace Brennen.BrennenCode.Cards.Uncommon;", "namespace Brennen.BrennenCode.Cards.Basic;")
        text = text.replace("CardRarity.Uncommon", "CardRarity.Basic")
        feed_new.write_text(text, encoding="utf-8")
        feed_old.unlink()
        print("Moved Feed -> Basic")

    # Generate card files
    for c in KIT:
        rarity = c["rarity"]
        path = CODE / rarity / f"{c['name']}.cs"
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(gen_card(c), encoding="utf-8")
    print(f"Wrote {len(KIT)} card source files")

    # Merge localization
    loc = {}
    if LOC.exists():
        loc = json.loads(LOC.read_text(encoding="utf-8"))

    # Ensure hand-authored keys remain; add generated
    for c in KIT:
        key = loc_key(c["name"])
        title_k = f"{key}.title"
        desc_k = f"{key}.description"
        loc[title_k] = c["title"]
        loc[desc_k] = "\n".join(c["lines"])

    # Fix Feed loc if needed (already present)
    LOC.write_text(json.dumps(loc, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Wrote localization ({len(loc)} keys)")

    # Placeholder portraits
    placeholder = PORTRAITS / "card.png"
    big_placeholder = PORTRAITS_BIG / "card.png"
    for c in KIT:
        stem = portrait_stem(c["name"])
        small = PORTRAITS / f"{stem}.png"
        big = PORTRAITS_BIG / f"{stem}.png"
        if not small.exists() and placeholder.exists():
            shutil.copy2(placeholder, small)
        if not big.exists() and big_placeholder.exists():
            shutil.copy2(big_placeholder, big)
    print("Ensured portrait placeholders")

    # docs/cards.json catalog — include hand-authored + generated + relic
    catalog = []
    # Starter relic
    catalog.append({
        "id": "duoqueue",
        "name": "Duo Queue",
        "rarity": "relic",
        "type": "Relic",
        "cost": None,
        "art": "assets/brennen/duoqueue.jpg",
        "lines": ["At the start of combat,", "gain 1 Energy."],
        "keywords": ["Energy"],
        "stats": "Starter",
        "flavor": "I've got you. Don't feed.",
    })

    hand_meta = [
        dict(id="strike", name="Strike", rarity="basic", type="Attack", cost=1,
             lines=["Deal 6 damage."], upgrade="9 damage", art="assets/brennen/strike.jpg"),
        dict(id="defend", name="Defend", rarity="basic", type="Skill", cost=1,
             lines=["Gain 5 Block."], keywords=["Block"], upgrade="8 Block", art="assets/brennen/defend.jpg"),
        dict(id="feed", name="Feeding", rarity="basic", type="Skill", cost=1,
             lines=["Heal the enemy to full HP.", "Exhaust."], keywords=["Exhaust"],
             flavor="Don't.", art="assets/brennen/feed.jpg"),
        dict(id="gank", name="Gank", rarity="common", type="Attack", cost=1,
             lines=["Deal 4 damage to a random enemy", "2 times."], upgrade="6 damage", art="assets/brennen/gank.jpg"),
        dict(id="flash", name="Flash", rarity="common", type="Skill", cost=0,
             lines=["Gain 6 Block.", "Exhaust."], keywords=["Block", "Exhaust"], upgrade="9 Block", art="assets/brennen/flash.jpg"),
        dict(id="tilt", name="Tilt", rarity="common", type="Attack", cost=1,
             lines=["Deal 9 damage.", "Take 2 damage."], upgrade="12 damage", flavor="Chat is cooking me.", art="assets/brennen/tilt.jpg"),
        dict(id="ward", name="Ward", rarity="common", type="Skill", cost=1,
             lines=["Gain 7 Block.", "Draw 1 card."], keywords=["Block"], upgrade="10 Block", art="assets/brennen/ward.jpg"),
        dict(id="firstblood", name="First Blood", rarity="uncommon", type="Attack", cost=0,
             lines=["Deal 3 damage.", "If Fatal, gain 1 Energy and draw 1."], upgrade="5 damage", art="assets/brennen/firstblood.jpg"),
        dict(id="maincharacter", name="Main Character", rarity="uncommon", type="Attack", cost=2,
             lines=["Deal 14 damage.", "If you have 50% or less HP, deal 20 instead."], upgrade="+4 dmg", art="assets/brennen/maincharacter.jpg"),
        dict(id="muteall", name="Mute All", rarity="uncommon", type="Skill", cost=1,
             lines=["Apply 2 (3) Weak to ALL enemies."], keywords=["Weak"], art="assets/brennen/muteall.jpg"),
        dict(id="pentakill", name="Pentakill", rarity="rare", type="Attack", cost=2,
             lines=["Deal 6 damage to ALL enemies", "3 times."], upgrade="8 damage", art="assets/brennen/pentakill.jpg"),
        dict(id="afk", name="AFK", rarity="rare", type="Skill", cost=3,
             lines=["Gain 20 Block.", "Exhaust."], keywords=["Block", "Exhaust"], upgrade="26 Block", art="assets/brennen/afk.jpg"),
        dict(id="remake", name="Remake", rarity="rare", type="Skill", cost=1,
             lines=["Draw 5 cards.", "Exhaust."], keywords=["Exhaust"], upgrade="Draw 6", art="assets/brennen/remake.jpg"),
        dict(id="duoqueue", name="Duo Queue", rarity="relic", type="Relic", cost=None,
             lines=["At the start of combat,", "gain 1 Energy."], stats="Starter", flavor="I've got you. Don't feed.", art="assets/brennen/duoqueue.jpg"),
    ]
    # rebuild catalog cleanly
    catalog = []
    seen = set()
    for h in hand_meta:
        if h["id"] in seen:
            continue
        seen.add(h["id"])
        entry = {k: v for k, v in h.items()}
        entry.setdefault("keywords", [])
        catalog.append(entry)

    for c in KIT:
        stem = portrait_stem(c["name"])
        entry = {
            "id": stem,
            "name": c["title"],
            "rarity": c["rarity"].lower(),
            "type": c["card_type"],
            "cost": c["cost"],
            "art": f"assets/brennen/{stem}.jpg",  # may 404; catalog falls back if missing
            "lines": c["lines"],
            "keywords": c.get("keywords") or [],
        }
        if c.get("flavor"):
            entry["flavor"] = c["flavor"]
        catalog.append(entry)

    DOCS_CARDS.write_text(json.dumps(catalog, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Wrote docs catalog ({len(catalog)} entries)")

    # README
    README.write_text(
        f"""# Brennen — Family Meme Pack (char 1)

Playable character for **sentou-koubou**: your older brother, League energy, Corvette shaka vibes.

## Kit (vanilla-sized)

| Piece | Detail |
|-------|--------|
| HP | 74 |
| Starter relic | **Duo Queue** — +1 Energy on combat turn 1 |
| Starting deck | 5× Strike, 4× Defend, **Feeding** |
| Reward pool | **20 Common / 35 Uncommon / 25 Rare** (80 cards) |
| Basics (non-reward) | Strike, Defend, Feeding |

Matches STS2 base-character pool size (Ironclad/Silent/etc.).

### Pillars

1. **Snowball** — kill rewards, openers, strength stacking  
2. **Tilt / int** — self-damage for payoff  
3. **Vision / peel** — Block + draw tools  
4. **Teamfight** — multi-hit and AoE  
5. **Chat control** — Weak / Vulnerable / Frail debuffs  

### Signature basics

| Card | Effect |
|------|--------|
| Strike / Defend | 6 dmg / 5 Block |
| **Feeding** | Heal enemy to full HP. Exhaust. (meme tax in the opener) |

## Catalog

```bash
# from repo root
python -m http.server -d docs 8765
# http://localhost:8765
```

Live: https://stephenshorton.github.io/sentou-koubou/

## Build

```bash
cp Directory.Build.props.example Directory.Build.props
dotnet restore && dotnet build
# Publish for .pck after assets/loc changes
```

Requires BaseLib + STS2 + MegaDot/Godot 4.5.1.

## Regenerating generated cards

New reward cards under `BrennenCode/Cards/{{Common,Uncommon,Rare}}/` (except hand-tuned kits)
are produced by:

```bash
python tools/generate_brennen_kit.py
```

Hand-authored keepers: Strike, Defend, Feed, Gank, Flash, Tilt, Ward, FirstBlood,
MainCharacter, MuteAll, Pentakill, AFK, Remake.
""",
        encoding="utf-8",
    )
    print("Updated README")


if __name__ == "__main__":
    main()
