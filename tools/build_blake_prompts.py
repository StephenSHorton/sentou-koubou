"""Blake M kit art prompts: presence variety + unique scene beats."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
cards = json.loads((ROOT / "docs/blake-cards.json").read_text(encoding="utf-8"))

STYLE = (
    "Official Slay the Spire 2 card portrait: bold hand-painted Mega Crit graphic style matching Blake M lock — "
    "soft matte gouache brush, simplified shapes, limited facial detail, strong silhouette, dark vignette, "
    "cobalt blue / gold / red gauntlet / cyan Charge palette. NOT anime, NOT photoreal, NOT Western comic ink. "
    "Landscape. No text, no UI, no green screen."
)

CHAR = (
    "SAME man as the reference: rectangular glasses, dark hair, short stubble, athletic face, "
    "cobalt blue racing pilot suit with gold pauldrons and trim, white pilot scarf, red-and-gold fighting gauntlets, "
    "cyan Charge energy on fists when charging/unleashing. Fists only — NEVER a sword or blade. "
    "Varied pose and expression — not the same ready stance every time. "
    "When in combat, body often orients right but face stays readable (three-quarter)."
)

UNIVERSE = (
    "Shared Blake racer-brawler universe props when relevant: red gauntlets, cyan Charge glow, white pilot scarf, "
    "gold racing trim, speed lines, checkered flag accents, trophy belt, booster coils, racing gloves, pit-crew tools. "
    "No greatsword, no dark plate tank armor."
)

# stem (lowercase no underscore) -> presence, scene beat, props
SCENES: dict[str, tuple[str, str, list[str]]] = {
    # basics
    "jab": ("full", "Blake snapping a sharp boxing jab with red gauntlets, quick punch trail, face readable three-quarter", ["gauntlet"]),
    "guard": ("full", "Blake raising both gauntlets in a tight guard, white scarf, defensive crouch different from ready", ["gauntlet", "block"]),
    "revup": ("full", "Blake winding the fist as cyan Charge doubles, energy swirling around gauntlet, intense focus", ["gauntlet", "charge"]),
    "haymaker": ("full", "Blake mid Unleash haymaker, cyan Charge releasing as a heavy punch, dynamic three-quarter", ["gauntlet", "charge"]),
    # commons
    "warmengine": ("full", "Blake revving safely behind a soft block aura, one fist glowing cyan, calm build-up pose", ["gauntlet", "block", "charge"]),
    "windup": ("partial", "Close on a cocked gauntlet with rising cyan Charge, Blake torso partial", ["gauntlet", "charge"]),
    "raptorboost": ("full", "Blake shoulder-rush boost attack then revving fist, speed lines, aggressive lean", ["gauntlet", "charge"]),
    "guardup": ("full", "Blake bracing harder guard while Charge glows above base, determined protector face", ["gauntlet", "block", "charge"]),
    "onetwo": ("full", "Blake double jab one-two combo blur, two punch trails, energetic grin", ["gauntlet"]),
    "dashattack": ("full", "Blake dashing low into a quick punch, motion blur speed, combo timing pose", ["gauntlet"]),
    "shouldercheck": ("full", "Blake shoulder check slam into foe silhouette, reading their attack intent, stern eyes", ["gauntlet"]),
    "falcondive": ("full", "Blake diving knee/fist aerial engage, YES! energy, kill-confirm spark", ["gauntlet"]),
    "sidestep": ("partial", "Blake silhouette slipping aside of a red attack telegraph, pocketed dodge", ["scarf"]),
    "showmeyourmoves": ("full", "Blake cocky taunt hands-beckoning 'show me your moves', scarf flaring, playful smirk", ["scarf"]),
    # uncommons — charge engines
    "fullthrottle": ("full", "Blake both fists blazing with double-Rev cyan Charge, pure commitment wind-up, fierce face", ["gauntlet", "charge"]),
    "ignition": ("partial", "Spark of ignition cyan on a gauntlet, Blake hand partial, free Rev flash", ["gauntlet", "charge"]),
    "redline": ("full", "Blake past the redline, Charge runaway engine glow, speed dial breaking, intense three-quarter", ["gauntlet", "charge"]),
    "slipstream": ("partial", "Speed cards streaming past Blake in a slipstream, partial figure, free Rev on the fourth beat", ["charge", "speed"]),
    "perfectshield": ("full", "Blake powershield parry pose, perfect block flash then fist revs, fighting-game read", ["gauntlet", "block"]),
    # uncommons — spend charge
    "pressure": ("full", "Blake pressure jab poking without releasing full Charge, half-power cyan fist, measured pose", ["gauntlet", "charge"]),
    "storedpower": ("partial", "Charge converted into a glowing block shell around a gauntlet, fist-as-shield still-life partial", ["gauntlet", "block", "charge"]),
    "falconkick": ("full", "Blake spinning falcon kick Unleash to all foes, multi-enemy shockwave, dynamic full body", ["gauntlet", "charge"]),
    "cleanko": ("full", "Blake precise KO punch sized exactly lethal, Charge preserved after kill spark, clinical finish", ["gauntlet", "charge"]),
    # uncommons — reads & tech
    "kneeofjustice": ("full", "Blake rising knee of justice on an attacking foe, electric sweetspot spark, punish pose", ["gauntlet"]),
    "grab": ("full", "Blake command grab crushing enemy shield/block, anti-armor clinch, fierce close-up", ["gauntlet"]),
    "spotdodge": ("partial", "Blake vanishing frame dodge, afterimage, invulnerable slip, partial figure", ["scarf"]),
    "yes": ("full", "Blake victory-lap YES! taunt after Unleash, arms wide, hype expression, confetti speed lines", ["scarf"]),
    "warmuplap": ("partial", "Track warm-up lap speed lines and card draw sparks, Blake small running figure", ["speed"]),
    # rares
    "falconpunch": ("full", "THE signature Falcon Punch wind-up then release, double Charge explosion, Follow-Through arc, epic three-quarter", ["gauntlet", "charge"]),
    "bluefalcon": ("full", "Blake as blue falcon final smash ramming all enemies, Charge kept loaded, vehicle-speed energy", ["charge", "speed"]),
    "gdiffuser": ("full", "Blake passive G-Diffuser aura auto-revving each turn, cyan engine rings around him, ominous build pose", ["charge"]),
    "superarmor": ("full", "Blake uninterruptible super armor glow, Charge immune to halving, tank-through stance fists up", ["gauntlet", "charge"]),
    "championsfist": ("partial", "Champion's fist raising base Charge permanently, trophy-gauntlet close-up with rising meter", ["gauntlet", "charge"]),
    "musclememory": ("full", "Blake muscle-memory multi-Rev after many Unleashes, ghost afterimages of prior punches, accelerated charge", ["gauntlet", "charge"]),
    "highlightreel": ("partial", "Highlight reel frames of past Unleashes refunding energy, film-strip speed, Blake small", ["charge"]),
    "heathaze": ("full", "Blake so hot while Revving that heat haze damages all, charging is the attack, shimmer AOE", ["gauntlet", "charge"]),
    "hardread": ("full", "Blake hard-reading an Attack intent, stunning the foe mid-button, psychic read pose finger point", ["gauntlet"]),
    "photofinish": ("full", "Blake photo finish checkered-flag Unleash KO on last enemy, gold and heal sparkle, victory line pose", ["gauntlet", "flag"]),
}


def stem_from_id(card_id: str) -> str:
    return card_id.replace("_", "").lower()


def build_prompt(presence: str, scene: str) -> str:
    if presence == "full":
        lead = f"{scene}. FEATURE {CHAR} as clear readable focus."
    elif presence == "partial":
        lead = (
            f"{scene}. Include {CHAR} as recognizable secondary figure or partial "
            f"(same glasses, blue racing suit, gauntlets), not always centered."
        )
    else:
        lead = (
            f"{scene}. Still-life or prop-forward Blake universe — no full face required. "
            f"Shared props only, same palette as the character lock."
        )
    return f"{lead} {UNIVERSE} {STYLE}"


def main() -> None:
    out_cards: dict = {}
    missing: list[str] = []
    for c in cards:
        stem = stem_from_id(c["id"])
        if stem not in SCENES:
            missing.append(stem)
            continue
        presence, scene, props = SCENES[stem]
        out_cards[stem] = {
            "presence": presence,
            "props": props,
            "name": c["title"],
            "rarity": c.get("rarity", ""),
            "prompt": build_prompt(presence, scene),
        }

    payload = {
        "character_ref": "docs/assets/blake/variants/blake_locked_portrait.jpg",
        "combat_ref": "docs/assets/blake/variants/blake_combat_right_green.jpg",
        "style_notes": "Blake M lock + lighter combat plate; fists only; cyan Charge signature",
        "cards": out_cards,
    }
    out_path = ROOT / "tools" / "blake_m_prompts.json"
    out_path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    print(f"wrote {out_path} cards={len(out_cards)} missing={missing}")


if __name__ == "__main__":
    main()
