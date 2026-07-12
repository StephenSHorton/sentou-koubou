"""Build Brennen tank-C6 card prompts from cards.json + visual bible."""
from __future__ import annotations
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
cards = json.loads((ROOT / "docs/cards.json").read_text(encoding="utf-8"))

STYLE = (
    "Official Slay the Spire 2 card portrait: bold hand-painted Mega Crit graphic style, "
    "simplified shapes, chunky brush, limited facial detail, strong silhouette, dark vignette, "
    "ember/charcoal/gold/purple palette. NOT anime, NOT photoreal. Landscape 1000x760 composition. "
    "No text, no UI, no card frame."
)

CHAR = (
    "SAME character as the reference image: bulkier tank man, spiky dark hair, neat goatee, "
    "fat dark plate armor with brass buckles and oversized pauldrons, one fat ember-glow greatsword "
    "(two-handed when armed, NO dagger). Varied pose and facial expression for this card — "
    "not the same frozen grin every time. When in combat scenes, he often faces toward the RIGHT "
    "(STS2 left-side fighter)."
)

UNIVERSE = (
    "Shared Brennen tank universe props when relevant: fat ember greatsword, heavy plate armor, "
    "pink control-ward crystal on iron stake, cracked gaming headset, gold first-blood coin, "
    "yellow smite lightning bolt, ranked metal badge, mute-X speaker disc, glowing shield bash."
)

# Scene beat + presence + props per card id
# presence: full | partial | none
SCENES: dict[str, tuple[str, str, list[str]]] = {
    # basics / core combat
    "strike": ("full", "Brennen mid heavy two-handed greatsword slash facing right, fat blade ember trail, bold tank swing", ["greatsword"]),
    "defend": ("full", "Brennen bracing in heavy plate, shield-bash forearm block / raised greatsword guard, yellow impact sparks, tank wall pose", ["plate", "greatsword"]),
    "feed": ("full", "Brennen overcommitting as tank into red danger marks with greatsword, reckless tank grin, still bulky protector energy", ["greatsword"]),
    "gank": ("full", "Brennen flanking from purple fog facing right, fat greatsword ready, pink ward glow behind, tank engage not assassin stab", ["greatsword", "pink_ward"]),
    "flash": ("full", "Brennen mid-blink white flash burst in plate armor, afterimage of fat pauldrons and greatsword", ["plate"]),
    "tilt": ("partial", "Cracked headset sparking, angry red chat aura, Brennen silhouette gripping greatsword frustrated", ["headset", "greatsword"]),
    "ward": ("none", "Glowing pink control-ward crystal on iron stake, fog brush, no character face — pure vision prop still-life", ["pink_ward"]),
    "firstblood": ("full", "Brennen after first kill facing right, fat greatsword dripping ember, huge gold first-blood coin floating", ["greatsword", "gold_coin"]),
    "maincharacter": ("full", "Brennen center stage in fat plate, spotlight, greatsword planted, main-character tank pose facing right", ["greatsword", "plate"]),
    "muteall": ("none", "Iron mute-X speaker disc crushing chat bubbles, cracked headset beside it, no character face", ["mute", "headset"]),
    "pentakill": ("full", "Brennen victorious tank with five skull tally marks on fat greatsword blade, gold coins, facing right", ["greatsword", "gold_coin"]),
    "afk": ("full", "Brennen slumped idle in heavy armor with greatsword leaning, AFK zzz, still same bulk design", ["greatsword", "plate"]),
    "remake": ("partial", "Broken ranked badge and cracked headset over grey fog, Brennen silhouette walking away with greatsword", ["rank_badge", "headset"]),
    "auto": ("partial", "Simple basic attack — fat greatsword tapping a small monster, Brennen as secondary figure", ["greatsword"]),
    "poke": ("partial", "Greatsword tip poke spark at range, Brennen partial figure facing right", ["greatsword"]),
    "camp": ("none", "Jungle camp totems and pink ward crystal in purple-green canopy, no face", ["pink_ward"]),
    "cs": ("none", "Gold coins raining from shattered minion silhouettes, fat greatsword tip in frame", ["gold_coin", "greatsword"]),
    "spamping": ("none", "Spam of glowing map pings as sharp icons, cracked headset, mute disc", ["headset", "mute"]),
    "report": ("partial", "Glowing report totem hammer stamp, Brennen silhouette with plate armor", ["rank_badge"]),
    "inting": ("full", "Brennen intentionally walking into enemy fire as meatshield tank, greatsword dragging, wild grin", ["greatsword", "plate"]),
    "boost": ("partial", "Ally silhouette powered by orange aura, Brennen partial tank bodyblocking in front", ["plate"]),
    "peel": ("full", "Brennen bodyblocking skillshots to protect fragile ally, fat plate facing right, greatsword intercept", ["plate", "greatsword"]),
    "facecheck": ("partial", "Brennen peeking into dark fog of war brush, pink ward faint, cautious tank face", ["pink_ward"]),
    "tax": ("partial", "Gold coins siphoned by greatsword tip, Brennen partial smug tank", ["gold_coin", "greatsword"]),
    "catch": ("full", "Brennen locking an enemy with taunt aura and greatsword hook, protective catch", ["greatsword"]),
    "smite": ("partial", "Huge yellow-white smite lightning bolt crushing objective, Brennen silhouette with greatsword", ["smite", "greatsword"]),
    "emote": ("full", "Brennen tank dance emote in fat armor with greatsword, playful expression (different from combat grin)", ["plate", "greatsword"]),
    "missing": ("none", "Missing ping icon over empty fog lane, pink ward dim", ["pink_ward"]),
    "controlward": ("none", "Bright pink control ward crystal on stake, purging vision fog", ["pink_ward"]),
    "snowball": ("partial", "Growing gold snowball of coins and power, Brennen tank silhouette riding momentum", ["gold_coin"]),
    "macro": ("none", "Glowing minimap with path lines and objective markers, fat greatsword hilt in corner", ["greatsword"]),
    "mentalboom": ("partial", "Headset exploding into red shards, Brennen clutching head in plate, stressed expression", ["headset", "plate"]),
    "inter": ("full", "Brennen sabotaging fight with wild greatsword swings, chaotic tank int energy", ["greatsword"]),
    "baron": ("partial", "Dark pit objective glow and smite bolt, Brennen tank silhouette at pit edge", ["smite"]),
    "drake": ("partial", "Dragon-shaped ember pit glow, Brennen partial with greatsword", ["greatsword", "smite"]),
    "splitpush": ("partial", "Lone tower silhouette under siege, Brennen tank pushing side with greatsword", ["greatsword"]),
    "tp": ("partial", "Blue teleport beacon circle, Brennen materializing in plate facing right", ["plate"]),
    "flashengage": ("full", "Brennen flash-in engage facing right, white flash + greatsword overhead tank start", ["greatsword", "plate"]),
    "chatrestrict": ("none", "Muted chat bubbles chained, mute-X disc, cracked headset", ["mute", "headset"]),
    "outplay": ("full", "Brennen tank dodging skillshot with plate, counter-greatsword flourish, cocky expression", ["greatsword", "plate"]),
    "kda": ("partial", "KDA scoreboard glow with gold coins, Brennen tank partial portrait", ["gold_coin"]),
    "shutdown": ("full", "Brennen landing shutdown blow with fat greatsword, bounty gold burst", ["greatsword", "gold_coin"]),
    "bounty": ("none", "Huge gold bounty bag and coin pile with greatsword scratch mark", ["gold_coin"]),
    "assist": ("partial", "Assist spark as Brennen tags kill for ally, plate tank secondary figure", ["greatsword", "gold_coin"]),
    "jungleclear": ("partial", "Clearing camp with greatsword sweeps, Brennen partial in canopy", ["greatsword"]),
    "backdoor": ("partial", "Sneaking to nexus crystal with greatsword, Brennen tank silhouette", ["greatsword"]),
    "freeze": ("none", "Frozen wave of ice-locked minion shapes under cold light, greatsword tip", ["greatsword"]),
    "rotate": ("partial", "Path lines across map, Brennen tank mid-rotate facing right", ["plate"]),
    "powerspike": ("partial", "Sudden power aura spike around fat plate, Brennen silhouette", ["plate"]),
    "itemspike": ("none", "Glowing legendary item icons as painted relics, gold and ember", ["gold_coin"]),
    "invade": ("full", "Brennen invading enemy jungle with greatsword, aggressive tank face right", ["greatsword"]),
    "wardhop": ("partial", "Brennen hopping over pink ward crystal with plate bulk", ["pink_ward", "plate"]),
    "allin": ("full", "Brennen full-commit tank charge facing right, greatsword and plate aura, everything committed", ["greatsword", "plate"]),
    "disrespect": ("full", "Brennen taunting with greatsword resting on shoulder, smug expression different from strike", ["greatsword"]),
    "peelbot": ("full", "Brennen as bot-lane bodyguard in plate, shielding ally silhouette, greatsword ready right", ["plate", "greatsword"]),
    "allchat": ("none", "Fiery all-chat bubbles and flame keycap prop, cracked headset", ["headset"]),
    "roamtimer": ("none", "Glowing clock and path line across fog map, pink ward", ["pink_ward"]),
    "deepward": ("none", "Deep pink ward in enemy jungle darkness", ["pink_ward"]),
    "doublebuff": ("partial", "Red and blue buff auras around Brennen tank silhouette", ["plate"]),
    "freeobj": ("partial", "Open objective pit uncontested, Brennen partial with smite glow", ["smite"]),
    "campsetup": ("none", "Camp setup wards and traps still-life, pink ward crystal", ["pink_ward"]),
    "ace": ("full", "Brennen after team wipe, five enemy silhouettes down, greatsword raised facing right", ["greatsword", "gold_coin"]),
    "pentasecure": ("full", "Brennen securing penta with decisive greatsword slam, gold rain", ["greatsword", "gold_coin"]),
    "onevnine": ("full", "Brennen alone vs many foe silhouettes as tank wall, plate and greatsword, desperate clutch face", ["greatsword", "plate"]),
    "uninstall": ("none", "Broken headset and uninstalled client crack, muted X", ["headset", "mute"]),
    "ggez": ("partial", "GG EZ badge glow, Brennen tank partial wave smug", ["rank_badge"]),
    "clutch": ("full", "Brennen barely surviving multi-foe fight as last tank standing, sparks, gritted teeth expression", ["greatsword", "plate"]),
    "perfectgame": ("partial", "Golden perfect laurel and zero-death glow, Brennen tank silhouette", ["gold_coin", "rank_badge"]),
    "intingsion": ("full", "Brennen zombie-tank unkillable vibe, greatsword and plate, manic expression", ["greatsword", "plate"]),
    "openmid": ("none", "Open mid lane path glowing empty after ff, mute disc", ["mute"]),
    "hardstuck": ("partial", "Cracked rank badge stuck in mud, Brennen sitting on plate armor frustrated", ["rank_badge", "plate"]),
    "fountaindive": ("full", "Brennen diving into glowing fountain beams with greatsword, reckless tank", ["greatsword"]),
    "fullclear": ("partial", "Empty cleared jungle camps, Brennen walking with greatsword", ["greatsword"]),
    "bait": ("full", "Brennen low-health bait tank glowing, enemies overcommit, protective taunt", ["plate"]),
    "throw": ("partial", "Rank badge tumbling into void, Brennen silhouette dropping lead", ["rank_badge"]),
    "topdiff": ("full", "Brennen towering over foe silhouette as top-lane tank diff, greatsword and plate", ["greatsword", "rank_badge"]),
    "jgdiff": ("full", "Brennen dominating jungle path as tank jungler vibe, smite bolt and greatsword", ["greatsword", "smite"]),
    "adcdiff": ("partial", "Ranged ally diff aura, Brennen peeling in front as tank secondary", ["plate"]),
    "supdiff": ("full", "Brennen as support-tank peeling, plate bodyblock, greatsword intercept", ["plate", "greatsword"]),
    "middiff": ("full", "Brennen mid-lane tank presence crushing foe, greatsword slash right", ["greatsword"]),
    "dodgethedodge": ("full", "Brennen reading a dodge, greatsword intercept timing, focused face", ["greatsword"]),
    "chatmod": ("none", "Moderator gavel and mute-X disc over chat bubbles", ["mute"]),
    "maincharactersyndrome": ("full", "Brennen hogging spotlight and kills, exaggerated main-character tank pose", ["greatsword", "gold_coin"]),
    "peelforadc": ("full", "Brennen smacking divers away from fragile ADC silhouette, fat plate and greatsword, protective fury", ["plate", "greatsword"]),
    "bodyblock": ("full", "Brennen fully bodyblocking a red skillshot beam for ally, fat pauldrons taking hit, facing right", ["plate"]),
    "teamfight": ("full", "Brennen frontlining teamfight, allies behind, greatsword and plate, facing right into enemy group", ["greatsword", "plate"]),
    "frontline": ("full", "Brennen as pure frontline wall, taunt aura rings, fat armor, greatsword planted, allies safe behind", ["plate", "greatsword"]),
    # extras that may appear in scenes but not cards.json
    "duoqueue": ("partial", "Two linked headset charms, Brennen and ally silhouette duo", ["headset"]),
    "ult": ("full", "Brennen ultimate tank slam with greatsword shockwave facing right", ["greatsword", "plate"]),
    "objective": ("partial", "Objective pit glow, Brennen tank contesting", ["smite"]),
    "ping": ("none", "Single glowing danger ping icon in fog", ["pink_ward"]),
    "zone": ("partial", "Brennen zoning space with greatsword sweeps, area denial arcs", ["greatsword"]),
    "roam": ("partial", "Brennen roaming river with plate, path lines", ["plate"]),
    "roambot": ("partial", "Brennen perpetual roam energy, map arrows, plate silhouette", ["plate"]),
    "visionscore": ("none", "Vision score glyphs and pink wards constellation", ["pink_ward"]),
    "diff": ("full", "Brennen aura of skill-diff with rank badge halo and greatsword", ["rank_badge", "greatsword"]),
    "challengerdiff": ("full", "Brennen with champion-tier glow, rank badge, greatsword facing right", ["rank_badge", "greatsword"]),
    "bounty": ("none", "Huge gold bounty sack, coin pile, greatsword scratch", ["gold_coin"]),
}

RELICS = {
    "duoqueue": "Single game relic artifact cutout: two linked dark headset charms on a short chain, brass hardware, transparent-ready pure green screen background, STS2 painted graphic prop only, no person, no scene",
    "rankedbadge": "Single relic artifact cutout: polished metal ranked badge with small ember star, green screen, STS2 painted prop only, no person",
    "reporttotem": "Single relic artifact cutout: small wooden report totem stamp with red X, green screen, STS2 painted prop only",
    "firstbloodcoin": "Single relic artifact cutout: thick gold first-blood coin with greatsword scratch, green screen, STS2 painted prop only",
    "mutecharm": "Single relic artifact cutout: iron mute-X speaker disc charm on leather cord, green screen, STS2 painted prop only",
    "wardtoken": "Single relic artifact cutout: pink control ward crystal token on iron base, green screen, STS2 painted prop only",
    "ggbadge": "Single relic artifact cutout: brass GG badge pin with ember edge, green screen, STS2 painted prop only",
    "flamekeycap": "Single relic artifact cutout: mechanical keyboard keycap with flame icon, ember glow, green screen, STS2 painted prop only",
}


def presence_clause(presence: str) -> str:
    if presence == "full":
        return f"FEATURE {CHAR} as clear readable focus."
    if presence == "partial":
        return (
            f"Include {CHAR} as a recognizable secondary figure or silhouette "
            "(same hair, goatee, fat plate, greatsword), not always centered."
        )
    return (
        "NO full character portrait — object/effect still-life only, same universe props and palette. "
        "May show a tiny distant silhouette at most."
    )


def build_prompt(scene: str, presence: str) -> str:
    return f"{scene}. {presence_clause(presence)} {UNIVERSE} {STYLE}"


def main():
    out: dict = {}
    for c in cards:
        cid = c["id"]
        if c.get("rarity") == "relic" or c.get("type") == "Relic":
            continue
        if cid not in SCENES:
            # fallback tank meme
            SCENES[cid] = (
                "partial",
                f"League-of-Legends meme card beat for '{c.get('name', cid)}' as tank-support fantasy with fat greatsword and plate motifs",
                ["greatsword", "plate"],
            )
        presence, scene, props = SCENES[cid]
        out[cid] = {
            "presence": presence,
            "props": props,
            "name": c.get("name", cid),
            "lines": c.get("lines", []),
            "prompt": build_prompt(scene, presence),
        }

    # include scene-only extras
    for cid, (presence, scene, props) in SCENES.items():
        if cid not in out:
            out[cid] = {
                "presence": presence,
                "props": props,
                "name": cid,
                "lines": [],
                "prompt": build_prompt(scene, presence),
            }

    relics = {k: {"prompt": v, "green_screen": True} for k, v in RELICS.items()}

    payload = {
        "character_ref": "tools/gen_out/cohesion/variants/brennen_fullbody_c6_green.jpg",
        "cards": out,
        "relics": relics,
    }
    path = ROOT / "tools" / "brennen_tank_c6_prompts.json"
    path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    print("wrote", path, "cards", len(out), "relics", len(relics))
    from collections import Counter
    print(Counter(v["presence"] for v in out.values()))


if __name__ == "__main__":
    main()
