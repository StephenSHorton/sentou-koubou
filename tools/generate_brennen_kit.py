#!/usr/bin/env python3
"""Generate Brennen's full vanilla-sized card pool from a kit definition.

Target (STS2 vanilla-ish):
  Basics: Strike, Defend, Feed (starter-only, not rewards)
  Starting deck: 5 Strike / 4 Defend / 1 Feed
  Rewards: 20 Common / 35 Uncommon / 25 Rare

Kit pass focus: cut template clones, real Powers (hooks), Role Diff jobs,
tank/peel fantasy + League meme packaging (solo Block/Weak + MP ally tools).
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
SCENES = ROOT / "tools" / "brennen_card_scenes.json"

# Hand-authored keepers (never overwritten by generator).
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
    "Peel",
    # Multiplayer tank tools (hand-authored; MultiplayerOnly)
    "PeelForAdc",
    "Bodyblock",
    "Teamfight",
    "Frontline",
}


def portrait_stem(name: str) -> str:
    return name.lower()


# ---------------------------------------------------------------------------
# Kit definition
# ---------------------------------------------------------------------------
# TargetType: AnyEnemy | AllEnemies | RandomEnemy | None
# type: Attack | Skill | Power
# tmpl: see gen_card()

KIT: list[dict] = [
    # ===== COMMON (16 gen + 4 hand = 20) =====
    # hand: Gank, Flash, Tilt, Ward
    # cut: Roam, Ping, Diff (pure clones)
    dict(name="Auto", rarity="Common", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_block", dmg=5, block=5, up_dmg=2, up_block=2,
         title="Auto", lines=["Deal {Damage:diff()} damage.", "Gain {Block:diff()} [gold]Block[/gold]."],
         flavor="Right-click the minion. With armor."),
    dict(name="Poke", rarity="Common", cost=0, card_type="Attack", target="AnyEnemy",
         tmpl="attack_draw", dmg=4, cards=1, up_dmg=2,
         title="Poke", lines=["Deal {Damage:diff()} damage.", "Draw {Cards:diff()} card."]),
    dict(name="Camp", rarity="Common", cost=1, card_type="Skill", target="None",
         tmpl="block", block=12, up_block=4,
         title="Camp", lines=["Gain {Block:diff()} [gold]Block[/gold]."],
         flavor="Bush checked."),
    dict(name="Cs", rarity="Common", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_block", dmg=5, block=5, up_dmg=2, up_block=2,
         title="CS", lines=["Deal {Damage:diff()} damage.", "Gain {Block:diff()} [gold]Block[/gold]."]),
    dict(name="SpamPing", rarity="Common", cost=1, card_type="Attack", target="RandomEnemy",
         tmpl="attack_hits", dmg=2, hits=4, up_hits=1,
         title="Spam Ping", lines=["Deal {Damage:diff()} damage to a random enemy {Repeat:diff()} times."],
         flavor="??? on CD."),
    dict(name="Report", rarity="Common", cost=0, card_type="Skill", target="AnyEnemy",
         tmpl="apply_weak", weak=3, up_weak=1,
         title="Report", lines=["Apply {Weak:diff()} [gold]Weak[/gold]."],
         flavor="After the game."),
    dict(name="Inting", rarity="Common", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_self", dmg=13, self_dmg=4, up_dmg=3,
         title="Inting", lines=["Deal {Damage:diff()} damage.", "Take [blue]4[/blue] damage."],
         flavor="Trust the process. For the team."),
    dict(name="Boost", rarity="Common", cost=0, card_type="Skill", target="None",
         tmpl="vigor", vigor=4, up_vigor=2,
         title="Boost", lines=["Gain {Vigor:diff()} [gold]Vigor[/gold]."]),
    dict(name="Peel", rarity="Common", cost=1, card_type="Skill", target="AnyEnemy",
         tmpl="block_weak", block=6, weak=1, up_block=3,
         title="Peel", lines=["Gain {Block:diff()} [gold]Block[/gold].", "Apply {Weak:diff()} [gold]Weak[/gold]."]),
    dict(name="Facecheck", rarity="Common", cost=1, card_type="Attack", target="AllEnemies",
         tmpl="attack", dmg=5, up_dmg=2,
         title="Facecheck", lines=["Deal {Damage:diff()} damage to ALL enemies."]),
    dict(name="Tax", rarity="Common", cost=1, card_type="Skill", target="None",
         tmpl="energy_next", energy=1, up_energy=1,
         title="Tax", lines=["Next turn, gain {Energy:energyIcons()}."],
         flavor="Lane tax."),
    dict(name="Catch", rarity="Common", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_weak", dmg=7, weak=2, up_dmg=3, up_weak=1,
         title="Catch", lines=["Deal {Damage:diff()} damage.", "Apply {Weak:diff()} [gold]Weak[/gold]."],
         flavor="Hook landed. Peel ready."),
    dict(name="Smite", rarity="Common", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_exhaust", dmg=14, up_dmg=4,
         title="Smite", lines=["Deal {Damage:diff()} damage."]),
    # NEW commons
    dict(name="Emote", rarity="Common", cost=0, card_type="Skill", target="AnyEnemy",
         tmpl="apply_weak_frail", weak=1, frail=1, up_weak=1,
         title="Emote", lines=["Apply {Weak:diff()} [gold]Weak[/gold] and {Frail:diff()} [gold]Frail[/gold]."],
         flavor="Mastery emote in fountain."),
    dict(name="Missing", rarity="Common", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_if_weak", dmg=6, weak_dmg=11, up_dmg=2,
         title="Missing",
         lines=["Deal {Damage:diff()} damage.", "If the enemy has [gold]Weak[/gold], deal {WeakDamage:diff()} instead."],
         flavor="ss mid."),
    dict(name="ControlWard", rarity="Common", cost=1, card_type="Skill", target="None",
         tmpl="block", block=8, up_block=3, keywords=["Retain"],
         title="Control Ward", lines=["Gain {Block:diff()} [gold]Block[/gold]."],
         flavor="Pink ward."),

    # ===== UNCOMMON (32 gen + 3 hand = 35) =====
    # hand: FirstBlood, MainCharacter, MuteAll
    # cut: Flame, Dive, Objective, RoamBot, Ult, VisionScore, Micro, PeelForAdc, Zone
    dict(name="Snowball", rarity="Uncommon", cost=1, card_type="Power", target="None",
         tmpl="brennen_power", power="SnowballPower", amount=6, up_amount=2, amount_key="Snowball",
         title="Snowball",
         lines=["Whenever you kill an enemy, gain {Snowball:diff()} [gold]Block[/gold]."],
         flavor="Don't throw the lead."),
    dict(name="Macro", rarity="Uncommon", cost=1, card_type="Power", target="None",
         tmpl="brennen_power", power="MacroPower", amount=1, up_amount=1, amount_key="Macro",
         title="Macro",
         lines=["At the start of your turn, draw {Macro:diff()} card."],
         flavor="Play the map."),
    dict(name="MentalBoom", rarity="Uncommon", cost=1, card_type="Power", target="None",
         tmpl="brennen_power", power="MentalBoomPower", amount=1, up_amount=1, amount_key="MentalBoom",
         title="Mental Boom",
         lines=["Whenever you lose HP on your turn, draw {MentalBoom:diff()} card."],
         flavor="Chat is cooking me."),
    dict(name="Inter", rarity="Uncommon", cost=1, card_type="Power", target="None",
         tmpl="brennen_power", power="InterPower", amount=3, up_amount=2, amount_key="Inter",
         title="Inter",
         lines=["Whenever you lose HP on your turn, gain {Inter:diff()} [gold]Vigor[/gold]."],
         flavor="For the team."),
    dict(name="Baron", rarity="Uncommon", cost=2, card_type="Attack", target="AnyEnemy",
         tmpl="attack_block", dmg=10, block=12, up_dmg=3, up_block=4,
         title="Baron", lines=["Deal {Damage:diff()} damage.", "Gain {Block:diff()} [gold]Block[/gold]."]),
    dict(name="Drake", rarity="Uncommon", cost=1, card_type="Skill", target="None",
         tmpl="block_draw", block=10, cards=2, up_block=3,
         title="Drake", lines=["Gain {Block:diff()} [gold]Block[/gold].", "Draw {Cards:diff()} cards."]),
    dict(name="SplitPush", rarity="Uncommon", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_block", dmg=8, block=6, up_dmg=3, up_block=2,
         title="Split Push", lines=["Deal {Damage:diff()} damage.", "Gain {Block:diff()} [gold]Block[/gold]."],
         flavor="Side lane pressure, full tank."),
    dict(name="Tp", rarity="Uncommon", cost=0, card_type="Skill", target="None",
         tmpl="block_exhaust", block=14, up_block=5,
         title="TP", lines=["Gain {Block:diff()} [gold]Block[/gold]."],
         flavor="Channeling..."),
    dict(name="FlashEngage", rarity="Uncommon", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_vuln", dmg=10, vuln=2, up_dmg=3,
         title="Flash Engage", lines=["Deal {Damage:diff()} damage.", "Apply {Vulnerable:diff()} [gold]Vulnerable[/gold]."]),
    dict(name="ChatRestrict", rarity="Uncommon", cost=1, card_type="Skill", target="AnyEnemy",
         tmpl="apply_frail", frail=2, up_frail=1,
         title="Chat Restrict", lines=["Apply {Frail:diff()} [gold]Frail[/gold]."]),
    dict(name="Outplay", rarity="Uncommon", cost=1, card_type="Skill", target="None",
         tmpl="draw_energy", cards=2, energy=1, up_cards=1,
         title="Outplay", lines=["Draw {Cards:diff()} cards.", "Gain {Energy:energyIcons()}."]),
    dict(name="Kda", rarity="Uncommon", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_hits", dmg=5, hits=3, up_dmg=2,
         title="KDA", lines=["Deal {Damage:diff()} damage {Repeat:diff()} times."]),
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
         title="Assist", lines=["Gain {Vigor:diff()} [gold]Vigor[/gold].", "Draw {Cards:diff()} card."],
         flavor="I was there."),
    dict(name="JungleClear", rarity="Uncommon", cost=1, card_type="Attack", target="AllEnemies",
         tmpl="attack", dmg=8, up_dmg=3,
         title="Jungle Clear", lines=["Deal {Damage:diff()} damage to ALL enemies."]),
    dict(name="Backdoor", rarity="Uncommon", cost=2, card_type="Attack", target="AnyEnemy",
         tmpl="attack_exhaust", dmg=24, up_dmg=6,
         title="Backdoor", lines=["Deal {Damage:diff()} damage."],
         flavor="They never look."),
    dict(name="Freeze", rarity="Uncommon", cost=1, card_type="Skill", target="AnyEnemy",
         tmpl="apply_weak_vuln", weak=1, vuln=1, up_weak=1,
         title="Freeze", lines=["Apply {Weak:diff()} [gold]Weak[/gold] and {Vulnerable:diff()} [gold]Vulnerable[/gold]."]),
    dict(name="Rotate", rarity="Uncommon", cost=1, card_type="Skill", target="None",
         tmpl="draw", cards=3, up_cards=1,
         title="Rotate", lines=["Draw {Cards:diff()} cards."]),
    dict(name="PowerSpike", rarity="Uncommon", cost=1, card_type="Skill", target="None",
         tmpl="strength_temp", strength=3, up_strength=1,
         title="Power Spike", lines=["Gain {Strength:diff()} [gold]Vigor[/gold]."],
         flavor="Item spike incoming."),
    dict(name="ItemSpike", rarity="Uncommon", cost=0, card_type="Skill", target="None",
         tmpl="energy", energy=2, up_energy=1, keywords=["Exhaust"],
         title="Item Spike", lines=["Gain {Energy:energyIcons()}."]),
    dict(name="Invade", rarity="Uncommon", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_weak", dmg=9, weak=2, up_dmg=3,
         title="Invade", lines=["Deal {Damage:diff()} damage.", "Apply {Weak:diff()} [gold]Weak[/gold]."]),
    dict(name="WardHop", rarity="Uncommon", cost=0, card_type="Skill", target="None",
         tmpl="block_draw", block=4, cards=1, up_block=2,
         title="Ward Hop", lines=["Gain {Block:diff()} [gold]Block[/gold].", "Draw {Cards:diff()} card."]),
    dict(name="AllIn", rarity="Uncommon", cost=2, card_type="Attack", target="AnyEnemy",
         tmpl="attack_hits", dmg=7, hits=3, up_dmg=2,
         title="All-In", lines=["Deal {Damage:diff()} damage {Repeat:diff()} times."]),
    dict(name="Disrespect", rarity="Uncommon", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_if_high_hp", dmg=6, high_dmg=14, up_dmg=2,
         title="Disrespect",
         lines=["Deal {Damage:diff()} damage.", "If you have more than [blue]50%[/blue] HP, deal {HighHpDamage:diff()} instead."],
         flavor="Dance in fountain."),
    # NEW uncommons
    dict(name="PeelBot", rarity="Uncommon", cost=1, card_type="Skill", target="None",
         tmpl="block_weak_all", block=10, weak=2, up_block=3, up_weak=1,
         title="Peel Bot",
         lines=["Gain {Block:diff()} [gold]Block[/gold].", "Apply {Weak:diff()} [gold]Weak[/gold] to ALL enemies."],
         flavor="Peel for ADC."),
    dict(name="AllChat", rarity="Uncommon", cost=1, card_type="Skill", target="AllEnemies",
         tmpl="apply_frail_all", frail=2, up_frail=1,
         title="All Chat", lines=["Apply {Frail:diff()} [gold]Frail[/gold] to ALL enemies."],
         flavor="Open mid."),
    dict(name="RoamTimer", rarity="Uncommon", cost=1, card_type="Attack", target="RandomEnemy",
         tmpl="attack_energy_next", dmg=8, energy=1, up_dmg=3,
         title="Roam Timer",
         lines=["Deal {Damage:diff()} damage to a random enemy.", "Next turn, gain {Energy:energyIcons()}."],
         flavor="Bot side missing."),
    dict(name="DeepWard", rarity="Uncommon", cost=1, card_type="Skill", target="None",
         tmpl="block", block=13, up_block=4, keywords=["Retain"],
         title="Deep Ward", lines=["Gain {Block:diff()} [gold]Block[/gold]."]),
    dict(name="DoubleBuff", rarity="Uncommon", cost=1, card_type="Skill", target="None",
         tmpl="block_draw", block=9, cards=1, up_block=3,
         title="Double Buff",
         lines=["Gain {Block:diff()} [gold]Block[/gold].", "Draw {Cards:diff()} card."],
         flavor="Red + blue. On a tank."),
        dict(name="FreeObj", rarity="Uncommon", cost=1, card_type="Skill", target="None",
         tmpl="block_draw", block=8, cards=2, up_block=3,
         title="Free Obj",
         lines=["Gain {Block:diff()} [gold]Block[/gold].", "Draw {Cards:diff()} cards."],
         flavor="They gave it for free. (Armor)"),
    dict(name="CampSetup", rarity="Uncommon", cost=1, card_type="Skill", target="None",
         tmpl="block_energy_next", block=7, energy=1, up_block=3,
         title="Camp Setup",
         lines=["Gain {Block:diff()} [gold]Block[/gold].", "Next turn, gain {Energy:energyIcons()}."]),

    # ===== RARE (22 gen + 3 hand = 25) =====
    # hand: Pentakill, Afk, Remake
    # cut: Quadra (overlap Full Clear / Pentakill)
    dict(name="Ace", rarity="Rare", cost=2, card_type="Attack", target="AllEnemies",
         tmpl="attack", dmg=14, up_dmg=4,
         title="Ace", lines=["Deal {Damage:diff()} damage to ALL enemies."]),
    dict(name="PentaSecure", rarity="Rare", cost=1, card_type="Power", target="None",
         tmpl="brennen_power", power="PentaSecurePower", amount=2, up_amount=1, amount_key="Penta",
         title="Penta Secure",
         lines=["Every time you play [blue]5[/blue] Attacks in a single turn, gain {Penta:diff()} [gold]Plating[/gold]."]),
    dict(name="OneVNine", rarity="Rare", cost=2, card_type="Attack", target="AnyEnemy",
         tmpl="attack_if_low_self", dmg=14, low_dmg=22, up_dmg=3,
         title="1v9",
         lines=["Deal {Damage:diff()} damage.", "If you have [blue]50%[/blue] or less HP, deal {LowHpDamage:diff()} instead."],
         flavor="Carry harder."),
    dict(name="Uninstall", rarity="Rare", cost=1, card_type="Attack", target="AnyEnemy",
         tmpl="attack_exhaust", dmg=22, up_dmg=6,
         title="Uninstall", lines=["Deal {Damage:diff()} damage."]),
    dict(name="GgEz", rarity="Rare", cost=0, card_type="Skill", target="AllEnemies",
         tmpl="apply_weak_vuln_all", weak=2, vuln=2, up_weak=1, keywords=["Exhaust"],
         title="GG EZ",
         lines=["Apply {Weak:diff()} [gold]Weak[/gold] and {Vulnerable:diff()} [gold]Vulnerable[/gold] to ALL enemies."],
         flavor="Honor me."),
    dict(name="Clutch", rarity="Rare", cost=1, card_type="Skill", target="None",
         tmpl="block_draw", block=18, cards=2, up_block=5,
         title="Clutch", lines=["Gain {Block:diff()} [gold]Block[/gold].", "Draw {Cards:diff()} cards."]),
        dict(name="PerfectGame", rarity="Rare", cost=2, card_type="Skill", target="None",
         tmpl="block_exhaust", block=28, up_block=8,
         title="Perfect Game", lines=["Gain {Block:diff()} [gold]Block[/gold]."],
         flavor="0 deaths. 12k damage taken."),
    dict(name="IntingSion", rarity="Rare", cost=2, card_type="Attack", target="AnyEnemy",
         tmpl="attack_self_big", dmg=30, self_dmg=8, up_dmg=8,
         title="Inting Sion", lines=["Deal {Damage:diff()} damage.", "Take [blue]8[/blue] damage."],
         flavor="Passive: for the team."),
    dict(name="OpenMid", rarity="Rare", cost=0, card_type="Skill", target="None",
         tmpl="draw_energy", cards=3, energy=2, up_cards=1, keywords=["Exhaust"],
         title="Open Mid", lines=["Draw {Cards:diff()} cards.", "Gain {Energy:energyIcons()}."]),
    dict(name="HardStuck", rarity="Rare", cost=2, card_type="Power", target="None",
         tmpl="brennen_power", power="HardStuckPower", amount=1, up_amount=1, amount_key="HardStuck",
         title="Hard Stuck",
         lines=["At the start of your turn, gain {HardStuck:diff()} [gold]Plating[/gold]."],
         flavor="Gold forever. Still tanking."),
    # Challenger Diff cut — pure big-number clone of Uninstall/1v9 ladder
    dict(name="FountainDive", rarity="Rare", cost=1, card_type="Attack", target="AllEnemies",
         tmpl="attack_self_aoe", dmg=12, self_dmg=5, up_dmg=3,
         title="Fountain Dive", lines=["Deal {Damage:diff()} damage to ALL enemies.", "Take [blue]5[/blue] damage."]),
        dict(name="FullClear", rarity="Rare", cost=2, card_type="Attack", target="AllEnemies",
         tmpl="attack_block", dmg=10, block=10, up_dmg=3, up_block=3,
         title="Full Clear", lines=["Deal {Damage:diff()} damage to ALL enemies.", "Gain {Block:diff()} [gold]Block[/gold]."],
         flavor="Clear camps. Clear waves. Clear the board."),
    dict(name="Bait", rarity="Rare", cost=1, card_type="Skill", target="None",
         tmpl="block_exhaust", block=25, up_block=8,
         title="Bait", lines=["Gain {Block:diff()} [gold]Block[/gold]."]),
    dict(name="Throw", rarity="Rare", cost=0, card_type="Skill", target="None",
         tmpl="self_dmg_energy", self_dmg=5, energy=3, up_energy=1, keywords=["Exhaust"],
         title="Throw", lines=["Take [blue]5[/blue] damage.", "Gain {Energy:energyIcons()}."],
         flavor="For the highlight."),
    # Role Diffs — five jobs, not five skins
    dict(name="TopDiff", rarity="Rare", cost=2, card_type="Attack", target="AnyEnemy",
         tmpl="attack_solo_bonus", dmg=14, solo_dmg=22, block=10, up_dmg=4, up_block=3,
         title="TOP Diff",
         lines=["Deal {Damage:diff()} damage.", "Gain {Block:diff()} [gold]Block[/gold].",
                "If there is only [blue]1[/blue] enemy, deal {SoloDamage:diff()} instead."],
         flavor="Island king."),
    dict(name="JgDiff", rarity="Rare", cost=1, card_type="Attack", target="RandomEnemy",
         tmpl="attack_hits_draw", dmg=5, hits=3, cards=1, up_dmg=2,
         title="JG Diff",
         lines=["Deal {Damage:diff()} damage to a random enemy {Repeat:diff()} times.", "Draw {Cards:diff()} card."],
         flavor="Pathing diff."),
    dict(name="AdcDiff", rarity="Rare", cost=1, card_type="Skill", target="None",
         tmpl="block_draw", block=14, cards=2, up_block=4,
         title="ADC Diff",
         lines=["Gain {Block:diff()} [gold]Block[/gold].", "Draw {Cards:diff()} cards."],
         flavor="I am the ADC now. (jk I peel)"),
    dict(name="SupDiff", rarity="Rare", cost=1, card_type="Skill", target="None",
         tmpl="block_weak_all", block=10, weak=2, up_block=4, up_weak=1,
         title="SUP Diff",
         lines=["Gain {Block:diff()} [gold]Block[/gold].", "Apply {Weak:diff()} [gold]Weak[/gold] to ALL enemies."],
         flavor="Engages for free."),
        dict(name="MidDiff", rarity="Rare", cost=1, card_type="Skill", target="None",
         tmpl="draw_energy", cards=2, energy=2, up_cards=1, keywords=["Exhaust"],
         title="MID Diff",
         lines=["Draw {Cards:diff()} cards.", "Gain {Energy:energyIcons()}."],
         flavor="Prio. Tank rotates first."),
    dict(name="DodgeTheDodge", rarity="Rare", cost=1, card_type="Skill", target="None",
         tmpl="block_draw", block=12, cards=3, up_block=4,
         title="Dodge the Dodge", lines=["Gain {Block:diff()} [gold]Block[/gold].", "Draw {Cards:diff()} cards."]),
    # NEW rares (replace Quadra)
    dict(name="ChatMod", rarity="Rare", cost=1, card_type="Power", target="None",
         tmpl="brennen_power", power="ChatModPower", amount=1, up_amount=1, amount_key="ChatMod",
         title="Chat Mod",
         lines=["At the start of your turn, apply {ChatMod:diff()} [gold]Weak[/gold] to ALL enemies."],
         flavor="Timeout applied."),
    dict(name="MainCharacterSyndrome", rarity="Rare", cost=1, card_type="Power", target="None",
         tmpl="brennen_power", power="MainCharacterPower", amount=3, up_amount=1, amount_key="MCS",
         title="Main Character Syndrome",
         lines=["Whenever you play a Skill, gain {MCS:diff()} [gold]Block[/gold]."],
         flavor="It's about me. And my shield."),
]


# ---------------------------------------------------------------------------
# Code generation
# ---------------------------------------------------------------------------

USINGS_BASE = """using BaseLib.Utils;
using Brennen.BrennenCode.Powers;
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

    props: list[str] = []
    vars_lines: list[str] = []
    on_play: list[str] = []
    on_up: list[str] = []

    keywords = list(c.get("keywords") or [])
    if tmpl in ("attack_exhaust", "block_exhaust") or "Exhaust" in keywords:
        if "Exhaust" not in keywords:
            keywords.append("Exhaust")
    if keywords:
        kws = ", ".join(f"CardKeyword.{k}" for k in keywords)
        props.append(f"    public override IEnumerable<CardKeyword> CanonicalKeywords => [{kws}];")

    if c["card_type"] == "Skill" and (
        tmpl.startswith("block") or tmpl in ("block_weak_all", "block_energy_next")
    ):
        props.append("    public override bool GainsBlock => true;")
    if tmpl in (
        "attack_block",
        "block",
        "block_draw",
        "block_weak",
        "block_exhaust",
        "attack_fatal_block",
        "block_weak_all",
        "block_energy_next",
        "attack_solo_bonus",
    ):
        if "GainsBlock" not in "\n".join(props):
            props.append("    public override bool GainsBlock => true;")

    tips: list[str] = []
    tip_blob = tmpl + " " + " ".join(str(x) for x in c.keys())
    if "weak" in tip_blob.lower() or "Weak" in str(c.get("lines")):
        tips.append("HoverTipFactory.FromPower<WeakPower>()")
    if "vuln" in tip_blob or "Vulnerable" in str(c.get("lines")):
        tips.append("HoverTipFactory.FromPower<VulnerablePower>()")
    if "frail" in tip_blob or "Frail" in str(c.get("lines")):
        tips.append("HoverTipFactory.FromPower<FrailPower>()")
    if "vigor" in tip_blob or "Vigor" in str(c.get("lines")):
        tips.append("HoverTipFactory.FromPower<VigorPower>()")
    if "strength" in tip_blob.lower() or "Strength" in str(c.get("lines")):
        tips.append("HoverTipFactory.FromPower<StrengthPower>()")
    # de-dupe tips
    seen_tips: list[str] = []
    for t in tips:
        if t not in seen_tips:
            seen_tips.append(t)
    if seen_tips:
        props.append(
            "    protected override IEnumerable<IHoverTip> ExtraHoverTips =>\n    [\n        "
            + ",\n        ".join(seen_tips)
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
    elif tmpl == "attack_hits_draw":
        add_dmg()
        add_repeat()
        add_cards()
        on_play.append("        await CommonActions.CardAttack(this, play)")
        on_play.append("            .WithHitCount(DynamicVars.Repeat.IntValue)")
        on_play.append("            .Execute(choiceContext);")
        on_play.append("        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);")
        up_dmg()
        up_hits()
        up_cards()
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
    elif tmpl == "attack_if_weak":
        add_dmg()
        vars_lines.append(f"        new DamageVar(\"WeakDamage\", {c['weak_dmg']}, ValueProp.Move),")
        on_play.append("""        var target = play.Target;
        var dmg = DynamicVars.Damage.BaseValue;
        if (target is not null && target.GetPower<WeakPower>() is not null)
            dmg = DynamicVars["WeakDamage"].BaseValue;
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
        on_up.append(f"        DynamicVars[\"WeakDamage\"].UpgradeValueBy({c.get('up_dmg', 2)}m);")
    elif tmpl == "attack_solo_bonus":
        add_dmg()
        add_block()
        vars_lines.append(f"        new DamageVar(\"SoloDamage\", {c['solo_dmg']}, ValueProp.Move),")
        on_play.append("""        var combat = Owner.Creature?.CombatState;
        var dmg = DynamicVars.Damage.BaseValue;
        if (combat is not null && combat.HittableEnemies.Count() == 1)
            dmg = DynamicVars["SoloDamage"].BaseValue;
        var stored = DynamicVars.Damage.BaseValue;
        DynamicVars.Damage.BaseValue = dmg;
        try
        {
            await CommonActions.CardAttack(this, play).Execute(choiceContext);
        }
        finally
        {
            DynamicVars.Damage.BaseValue = stored;
        }
        await CommonActions.CardBlock(this, play);""")
        up_dmg()
        up_block()
        on_up.append(f"        DynamicVars[\"SoloDamage\"].UpgradeValueBy({c.get('up_dmg', 4)}m);")
    elif tmpl == "attack_fatal_block":
        add_dmg()
        add_block()
        on_play.append("        await CommonActions.CardAttack(this, play).Execute(choiceContext);")
        on_play.append("""        if (play.Target is not null && play.Target.IsDead)
            await CommonActions.CardBlock(this, play);""")
        up_dmg()
        up_block()
    elif tmpl == "attack_fatal_energy":
        add_dmg()
        add_energy()
        on_play.append("        await CommonActions.CardAttack(this, play).Execute(choiceContext);")
        on_play.append("""        var combat = Owner.Creature?.CombatState;
        if (combat is not null && combat.HittableEnemies.Any(e => e.IsDead) == false)
        {
            // Fatal check: any enemy killed by this play — use Target if single, else scan
        }
        if (play.Target is not null && play.Target.IsDead)
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        else if (combat is not null)
        {
            // AoE: if any enemy died this play we can't easily know; check all dead that still in list is wrong.
            // Fallback: grant energy if fewer living enemies than before is hard; skip if no target.
        }""")
        # Cleaner AoE fatal: track pre-play HP via AttackCommand — keep simple: energy if ANY enemy is dead after
        on_play.clear()
        on_play.append("""        var combat = Owner.Creature?.CombatState;
        var livingBefore = combat?.HittableEnemies.Count() ?? 0;
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        var livingAfter = combat?.HittableEnemies.Count() ?? 0;
        if (livingAfter < livingBefore)
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);""")
        up_dmg()
        up_energy()
    elif tmpl == "attack_energy_next":
        add_dmg()
        add_energy()
        on_play.append("        await CommonActions.CardAttack(this, play).Execute(choiceContext);")
        on_play.append(apply_power_self("EnergyNextTurnPower", "DynamicVars.Energy.IntValue"))
        up_dmg()
        up_energy()
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
    elif tmpl == "block_weak_all":
        add_block()
        vars_lines.append(f"        new DynamicVar(\"Weak\", {c['weak']}),")
        on_play.append("        await CommonActions.CardBlock(this, play);")
        on_play.append(apply_all("WeakPower", "DynamicVars[\"Weak\"].IntValue"))
        up_block()
        if c.get("up_weak"):
            on_up.append(f"        DynamicVars[\"Weak\"].UpgradeValueBy({c['up_weak']}m);")
    elif tmpl == "block_energy_next":
        add_block()
        add_energy()
        on_play.append("        await CommonActions.CardBlock(this, play);")
        on_play.append(apply_power_self("EnergyNextTurnPower", "DynamicVars.Energy.IntValue"))
        up_block()
        up_energy()
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
    elif tmpl == "apply_frail_all":
        vars_lines.append(f"        new DynamicVar(\"Frail\", {c['frail']}),")
        on_play.append(apply_all("FrailPower", "DynamicVars[\"Frail\"].IntValue"))
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
    elif tmpl == "apply_weak_frail":
        vars_lines.append(f"        new DynamicVar(\"Weak\", {c['weak']}),")
        vars_lines.append(f"        new DynamicVar(\"Frail\", {c['frail']}),")
        on_play.append(apply_power("WeakPower", "DynamicVars[\"Weak\"].IntValue"))
        on_play.append(apply_power("FrailPower", "DynamicVars[\"Frail\"].IntValue"))
        if c.get("up_weak"):
            on_up.append(f"        DynamicVars[\"Weak\"].UpgradeValueBy({c['up_weak']}m);")
            on_up.append(f"        DynamicVars[\"Frail\"].UpgradeValueBy({c['up_weak']}m);")
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
    elif tmpl == "vigor_strength":
        vars_lines.append(f"        new DynamicVar(\"Vigor\", {c['vigor']}),")
        vars_lines.append(f"        new DynamicVar(\"Strength\", {c['strength']}),")
        on_play.append(apply_power_self("VigorPower", "DynamicVars[\"Vigor\"].IntValue"))
        on_play.append(apply_power_self("StrengthPower", "DynamicVars[\"Strength\"].IntValue"))
        if c.get("up_vigor"):
            on_up.append(f"        DynamicVars[\"Vigor\"].UpgradeValueBy({c['up_vigor']}m);")
        if c.get("up_strength"):
            on_up.append(f"        DynamicVars[\"Strength\"].UpgradeValueBy({c['up_strength']}m);")
    elif tmpl == "brennen_power":
        key = c["amount_key"]
        vars_lines.append(f"        new DynamicVar(\"{key}\", {c['amount']}),")
        power = c["power"]
        on_play.append(apply_power_self(power, f"DynamicVars[\"{key}\"].IntValue"))
        if c.get("up_amount"):
            on_up.append(f"        DynamicVars[\"{key}\"].UpgradeValueBy({c['up_amount']}m);")
    elif tmpl == "power_strength":
        vars_lines.append(f"        new DynamicVar(\"Strength\", {c['strength']}),")
        on_play.append(apply_power_self("StrengthPower", "DynamicVars[\"Strength\"].IntValue"))
        if c.get("up_strength"):
            on_up.append(f"        DynamicVars[\"Strength\"].UpgradeValueBy({c['up_strength']}m);")
    elif tmpl == "strength_temp":
        vars_lines.append(f"        new DynamicVar(\"Strength\", {c['strength']}),")
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

    # usings for LINQ when needed
    needs_linq = tmpl in ("attack_solo_bonus", "attack_fatal_energy")
    usings = USINGS_BASE
    if needs_linq:
        usings = "using System.Linq;\n" + usings
    if tmpl == "attack_if_weak":
        usings = USINGS_BASE  # WeakPower already imported via Models.Powers

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

    return f"""{HEADER}{usings}
namespace {ns};

public sealed class {name}() : BrennenCard({cost}, {ctype}, {crarity}, {target})
{{{props_block}{vars_block}{on_play_block}{on_up_block}}}
"""


def loc_key(name: str) -> str:
    snake = []
    for i, ch in enumerate(name):
        if ch.isupper() and i:
            snake.append("_")
        snake.append(ch.upper())
    return "BRENNEN-" + "".join(snake)


def catalog_lines(c: dict) -> list[str]:
    """Human-readable catalog lines (resolve {Var:diff()} to kit numbers when possible)."""
    raw = list(c["lines"])
    # Keep dynamic-style text for catalog consistency with game loc
    return raw


def main() -> None:
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

    kit_names = {c["name"] for c in KIT}

    # Remove orphaned generated card sources (not hand-authored, not in KIT)
    for rarity in ("Common", "Uncommon", "Rare", "Basic"):
        folder = CODE / rarity
        if not folder.is_dir():
            continue
        for path in folder.glob("*.cs"):
            stem = path.stem
            if stem in HAND_AUTHORED:
                continue
            if stem in kit_names:
                continue
            path.unlink()
            print(f"Removed orphan {path.relative_to(ROOT)}")

    # Generate card files
    for c in KIT:
        rarity = c["rarity"]
        path = CODE / rarity / f"{c['name']}.cs"
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(gen_card(c), encoding="utf-8")
    print(f"Wrote {len(KIT)} card source files")

    # Merge localization — rebuild generated keys, keep hand keys
    loc: dict = {}
    if LOC.exists():
        loc = json.loads(LOC.read_text(encoding="utf-8"))

    # Drop loc keys for removed generated cards
    keep_prefixes = {loc_key(n) for n in kit_names | HAND_AUTHORED}
    # HAND uses class names; also StrikeBrennen etc.
    keep_prefixes.add(loc_key("StrikeBrennen"))
    keep_prefixes.add(loc_key("DefendBrennen"))
    keep_prefixes.add(loc_key("Feed"))
    for k in list(loc.keys()):
        prefix = k.rsplit(".", 1)[0]
        # Keep if any keep prefix matches
        if not any(prefix == p or prefix.startswith(p) for p in keep_prefixes):
            # only strip BRENNEN- card keys we no longer generate
            if k.startswith("BRENNEN-") and (k.endswith(".title") or k.endswith(".description")):
                # check if card still exists
                card_id = prefix.removeprefix("BRENNEN-")
                # map STRIKE_BRENNEN etc.
                pass  # keep all hand-ish; clean only known cuts later

    for c in KIT:
        key = loc_key(c["name"])
        loc[f"{key}.title"] = c["title"]
        loc[f"{key}.description"] = "\n".join(c["lines"])

    # Explicitly drop cut cards' loc
    for cut in (
        "ROAM", "PING", "DIFF", "FLAME", "DIVE", "OBJECTIVE", "ROAM_BOT", "ULT",
        "VISION_SCORE", "MICRO", "PEEL_FOR_ADC", "ZONE", "QUADRA", "CHALLENGER_DIFF",
    ):
        loc.pop(f"BRENNEN-{cut}.title", None)
        loc.pop(f"BRENNEN-{cut}.description", None)

    LOC.write_text(json.dumps(loc, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Wrote localization ({len(loc)} keys)")

    # Placeholder portraits for new cards only
    placeholder = PORTRAITS / "card.png"
    big_placeholder = PORTRAITS_BIG / "card.png"
    missing_art: list[str] = []
    for c in KIT:
        stem = portrait_stem(c["name"])
        small = PORTRAITS / f"{stem}.png"
        big = PORTRAITS_BIG / f"{stem}.png"
        if not small.exists():
            missing_art.append(stem)
            if placeholder.exists():
                shutil.copy2(placeholder, small)
        if not big.exists() and big_placeholder.exists():
            shutil.copy2(big_placeholder, big)
    if missing_art:
        print(f"New cards needing real art ({len(missing_art)}): {', '.join(missing_art)}")
    else:
        print("All kit portraits present")

    # docs/cards.json catalog
    hand_meta = [
        dict(id="strike", name="Strike", rarity="basic", type="Attack", cost=1,
             lines=["Deal 6 damage."], upgrade="9 damage", art="assets/brennen/strike.jpg"),
        dict(id="defend", name="Defend", rarity="basic", type="Skill", cost=1,
             lines=["Gain 5 Block."], keywords=["Block"], upgrade="8 Block", art="assets/brennen/defend.jpg"),
        dict(id="feed", name="Feeding", rarity="basic", type="Skill", cost=1,
             lines=["Heal the enemy to full HP."], keywords=["Exhaust"],
             flavor="Don't.", art="assets/brennen/feed.jpg"),
        dict(id="gank", name="Gank", rarity="common", type="Attack", cost=1,
             lines=["Deal 4 damage to a random enemy", "2 times."], upgrade="6 damage", art="assets/brennen/gank.jpg"),
        dict(id="flash", name="Flash", rarity="common", type="Skill", cost=0,
             lines=["Gain 6 Block."], keywords=["Block", "Exhaust"], upgrade="9 Block", art="assets/brennen/flash.jpg"),
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
             lines=["Gain 20 Block."], keywords=["Block", "Exhaust"], upgrade="26 Block", art="assets/brennen/afk.jpg"),
        dict(id="remake", name="Remake", rarity="rare", type="Skill", cost=1,
             lines=["Draw 5 cards."], keywords=["Exhaust"], upgrade="Draw 6", art="assets/brennen/remake.jpg"),
        dict(id="duoqueue", name="Duo Queue", rarity="relic", type="Relic", cost=None,
             lines=["At the start of combat,", "gain 1 Energy."], stats="Starter", flavor="I've got you. Don't feed.", art="assets/brennen/duoqueue.jpg"),
    ]
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
            "art": f"assets/brennen/{stem}.jpg",
            "lines": catalog_lines(c),
            "keywords": c.get("keywords") or [],
        }
        if c.get("flavor"):
            entry["flavor"] = c["flavor"]
        catalog.append(entry)

    DOCS_CARDS.write_text(json.dumps(catalog, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Wrote docs catalog ({len(catalog)} entries)")

    # Scene prompts for new cards (art pipeline)
    scenes = {}
    if SCENES.exists():
        try:
            scenes = json.loads(SCENES.read_text(encoding="utf-8"))
        except json.JSONDecodeError:
            scenes = {}
    new_scenes = {
        "emote": "League-style mastery emote flex in enemy fountain, cocky pose, neon UI, card portrait",
        "missing": "jungler silhouette missing from lane, question marks, fog of war, card portrait",
        "controlward": "pink control ward glowing in a brush, vision denied, card portrait",
        "mentalboom": "gamer raging at chat, red chat log, cracked headset, card portrait",
        "inter": "ally diving fountain for the team, sacrificial play, card portrait",
        "peelbot": "support bodyblocking skillshots for ADC, protective stance, card portrait",
        "allchat": "all-chat keyboard slam, toxic speech bubbles, card portrait",
        "roamtimer": "minimap roam path with countdown timer, gank setup, card portrait",
        "deepward": "deep tri-bush ward placement at night, card portrait",
        "doublebuff": "red and blue buff spirits both claimed, glowing, card portrait",
        "freeobj": "unguarded dragon pit objective, free take, card portrait",
        "campsetup": "jungle camp stacked for gank, vision lines, card portrait",
        "chatmod": "mod hammer slamming chat, mute icons, card portrait",
        "maincharactersyndrome": "spotlight on one player hogging the camera, main character energy, card portrait",
    }
    for k, v in new_scenes.items():
        scenes.setdefault(k, v)
    SCENES.write_text(json.dumps(scenes, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Updated scene prompts ({len(scenes)} entries)")

    power_count = sum(1 for c in KIT if c["card_type"] == "Power")
    README.write_text(
        f"""# Brennen — Family Meme Pack (char 1)

Playable character for **sentou-koubou**: your older brother, League energy, Corvette shaka vibes.

## Kit (vanilla-sized, intentionally OP meme)

| Piece | Detail |
|-------|--------|
| HP | 74 |
| Starter relic | **Duo Queue** — +1 Energy on combat turn 1 |
| Starting deck | 5× Strike, 4× Defend, **Feeding** |
| Reward pool | **20 Common / 35 Uncommon / 25 Rare** (80 cards) |
| Basics (non-reward) | Strike, Defend, Feeding |
| Powers | **{power_count}** generated power cards + custom hook powers |

### Pillars

1. **Snowball** — kills grant Strength, Fatal payoffs, First Blood  
2. **Tilt / int** — self-damage package (Tilt, Inting, Mental Boom, Inter)  
3. **Vision / peel** — Wards, Retain Block, Peel Bot  
4. **Teamfight** — AoE, Pentakill, Full Clear, Penta Secure  
5. **Chat control** — Weak / Frail / Mute All / Chat Mod / GG EZ  

### Signature basics

| Card | Effect |
|------|--------|
| Strike / Defend | 6 dmg / 5 Block |
| **Feeding** | Heal enemy to full HP. Exhaust. (meme tax in the opener) |

### Role Diffs (rares)

| Diff | Job |
|------|-----|
| TOP Diff | Island 1v1 — big hit + Block, bonus if only 1 enemy |
| JG Diff | Pathing — random multi-hit + draw |
| MID Diff | Prio — draw + energy (Exhaust) |
| ADC Diff | DPS — high multi-hit |
| SUP Diff | Peel — Block + Weak ALL |

## Catalog

```bash
python -m http.server -d docs 8765
# http://localhost:8765
```

Live: https://stephenshorton.github.io/sentou-koubou/

## Build

```bash
cp Directory.Build.props.example Directory.Build.props
dotnet restore && dotnet build
```

## Regenerating

```bash
python tools/generate_brennen_kit.py
```

Hand-authored keepers: Strike, Defend, Feed, Gank, Flash, Tilt, Ward, FirstBlood,
MainCharacter, MuteAll, Pentakill, AFK, Remake.

Custom powers live under `BrennenCode/Powers/` (Snowball, Macro, Mental Boom, Inter,
Penta Secure, Hard Stuck, Chat Mod, Main Character Syndrome).
""",
        encoding="utf-8",
    )
    print("Updated README")


if __name__ == "__main__":
    main()
