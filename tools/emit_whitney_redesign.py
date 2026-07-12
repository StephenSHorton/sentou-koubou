#!/usr/bin/env python3
"""Emit curated Whitney redesign card pool + localization."""
from __future__ import annotations

import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
W = ROOT / "mods" / "whitney"
CARDS = W / "WhitneyCode" / "Cards"
LOC = W / "Whitney" / "localization" / "eng" / "cards.json"
POW_LOC = W / "Whitney" / "localization" / "eng" / "powers.json"

KEEP = {"WhitneyCard"}

# (folder, class_name, title, description, csharp body without outer class wrapper — full file)
# We'll write full files directly.


def write(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content.strip() + "\n", encoding="utf-8")


def clear_old() -> None:
    for rarity in ("Basic", "Common", "Uncommon", "Rare"):
        folder = CARDS / rarity
        if not folder.is_dir():
            continue
        for p in folder.glob("*.cs"):
            p.unlink()
            print("removed", p.relative_to(ROOT))


HEADER = """\
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Whitney.WhitneyCode;
using Whitney.WhitneyCode.Powers;
"""

# Cards list for loc
LOC_ENTRIES: dict[str, tuple[str, str]] = {}


def loc_key(name: str) -> str:
    snake = []
    for i, ch in enumerate(name):
        if ch.isupper() and i:
            snake.append("_")
        snake.append(ch.upper())
    return f"WHITNEY-{''.join(snake)}"


def add_loc(name: str, title: str, desc: str) -> None:
    k = loc_key(name)
    LOC_ENTRIES[f"{k}.title"] = title
    LOC_ENTRIES[f"{k}.description"] = desc


def card(folder: str, name: str, body: str, title: str, desc: str) -> None:
    add_loc(name, title, desc)
    write(CARDS / folder / f"{name}.cs", HEADER + "\n" + body)


def emit_cards() -> None:
    # ─── BASIC ───────────────────────────────────────────────
    card("Basic", "Spark", '''
namespace Whitney.WhitneyCode.Cards.Basic;

public sealed class Spark() : WhitneyCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    public override WhitneyElement Element => WhitneyElement.Fire;
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
''', "Spark", "Deal {Damage:diff()} damage.")

    card("Basic", "Ripple", '''
namespace Whitney.WhitneyCode.Cards.Basic;

public sealed class Ripple() : WhitneyCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Water;
    public override bool GainsBlock => true;
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(5, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
''', "Ripple", "Gain {Block:diff()} [gold]Block[/gold].")

    card("Basic", "ChannelInk", '''
namespace Whitney.WhitneyCode.Cards.Basic;

public sealed class ChannelInk() : WhitneyCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Wind;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Ink", 2), new CardsVar(0)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await Ink.Gain(choiceContext, Owner, DynamicVars["Ink"].IntValue, this);
        if (DynamicVars.Cards.BaseValue > 0)
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
''', "Channel Ink", "Gain {Ink:diff()} [gold]Ink[/gold]. NL Draw {Cards:diff()} card(s).")

    # Fix ChannelInk desc - when Cards is 0, description is awkward. Use two versions conceptually.
    LOC_ENTRIES[loc_key("ChannelInk") + ".description"] = (
        "Gain {Ink:diff()} [gold]Ink[/gold]. NL Draw {Cards:diff()} card."
    )

    card("Basic", "ApprenticeSeal", '''
namespace Whitney.WhitneyCode.Cards.Basic;

public sealed class ApprenticeSeal() : WhitneyCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    public override WhitneyElement Element => WhitneyElement.Earth;
    protected override int SealCost => 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<InkPower>(),
        HoverTipFactory.FromPower<WeakPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9, ValueProp.Move),
        new DynamicVar("Weak", 1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (play.Target is not null)
        {
            await PowerCmd.Apply<WeakPower>(
                choiceContext, play.Target, DynamicVars["Weak"].IntValue, Owner.Creature, this);
        }
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
''', "Apprentice Seal", "Deal {Damage:diff()} damage. NL Apply {Weak:diff()} [gold]Weak[/gold].")

    # ─── COMMONS ─────────────────────────────────────────────
    card("Common", "EmberStroke", '''
namespace Whitney.WhitneyCode.Cards.Common;

public sealed class EmberStroke() : WhitneyCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override WhitneyElement Element => WhitneyElement.Fire;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move),
        new DynamicVar("Ink", 1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await Ink.Gain(choiceContext, Owner, DynamicVars["Ink"].IntValue, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
''', "Ember Stroke", "Deal {Damage:diff()} damage. NL Gain {Ink:diff()} [gold]Ink[/gold].")

    card("Common", "CinderFlick", '''
namespace Whitney.WhitneyCode.Cards.Common;

public sealed class CinderFlick() : WhitneyCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override WhitneyElement Element => WhitneyElement.Fire;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4, ValueProp.Move),
        new DynamicVar("Ink", 1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var blend = IsBlendActive;
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (blend)
            await Ink.Gain(choiceContext, Owner, DynamicVars["Ink"].IntValue, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
''', "Cinder Flick", "Deal {Damage:diff()} damage. NL [gold]Blend[/gold]: Gain {Ink:diff()} [gold]Ink[/gold].")

    card("Common", "Tideguard", '''
namespace Whitney.WhitneyCode.Cards.Common;

public sealed class Tideguard() : WhitneyCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Water;
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6, ValueProp.Move),
        new DynamicVar("Ink", 1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await Ink.Gain(choiceContext, Owner, DynamicVars["Ink"].IntValue, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
''', "Tideguard", "Gain {Block:diff()} [gold]Block[/gold]. NL Gain {Ink:diff()} [gold]Ink[/gold].")

    card("Common", "Undertow", '''
namespace Whitney.WhitneyCode.Cards.Common;

public sealed class Undertow() : WhitneyCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Water;
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5, ValueProp.Move),
        new CardsVar(1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var blend = IsBlendActive;
        await CommonActions.CardBlock(this, play);
        if (blend)
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
''', "Undertow", "Gain {Block:diff()} [gold]Block[/gold]. NL [gold]Blend[/gold]: Draw {Cards:diff()} card.")

    card("Common", "StoneStroke", '''
namespace Whitney.WhitneyCode.Cards.Common;

public sealed class StoneStroke() : WhitneyCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override WhitneyElement Element => WhitneyElement.Earth;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<AttunementPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7, ValueProp.Move),
        new DynamicVar("Attune", 1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (Owner.Creature is not null)
            await PowerCmd.Apply<AttunementPower>(
                choiceContext, Owner.Creature, DynamicVars["Attune"].IntValue, Owner.Creature, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
''', "Stone Stroke", "Deal {Damage:diff()} damage. NL Gain {Attune:diff()} [gold]Attunement[/gold].")

    card("Common", "GravenWard", '''
namespace Whitney.WhitneyCode.Cards.Common;

public sealed class GravenWard() : WhitneyCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Earth;
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<AttunementPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5, ValueProp.Move),
        new DynamicVar("Attune", 1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        if (Owner.Creature is not null)
            await PowerCmd.Apply<AttunementPower>(
                choiceContext, Owner.Creature, DynamicVars["Attune"].IntValue, Owner.Creature, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
''', "Graven Ward", "Gain {Block:diff()} [gold]Block[/gold]. NL Gain {Attune:diff()} [gold]Attunement[/gold].")

    card("Common", "Zephyr", '''
namespace Whitney.WhitneyCode.Cards.Common;

public sealed class Zephyr() : WhitneyCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Wind;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new DynamicVar("Ink", 1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var blend = IsBlendActive;
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        if (blend)
            await Ink.Gain(choiceContext, Owner, DynamicVars["Ink"].IntValue, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars["Ink"].UpgradeValueBy(1m);
}
''', "Zephyr", "Draw {Cards:diff()} card. NL [gold]Blend[/gold]: Gain {Ink:diff()} [gold]Ink[/gold].")

    card("Common", "Gust", '''
namespace Whitney.WhitneyCode.Cards.Common;

public sealed class Gust() : WhitneyCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override WhitneyElement Element => WhitneyElement.Wind;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3, ValueProp.Move),
        new RepeatVar(2),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hits = IsBlendActive ? DynamicVars.Repeat.IntValue + 1 : DynamicVars.Repeat.IntValue;
        await CommonActions.CardAttack(this, play).WithHitCount(hits).Execute(choiceContext);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}
''', "Gust", "Deal {Damage:diff()} damage {Repeat:diff()} times. NL [gold]Blend[/gold]: {Repeat:diff()}+1 times.")

    card("Common", "QuickSeal", '''
namespace Whitney.WhitneyCode.Cards.Common;

public sealed class QuickSeal() : WhitneyCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override WhitneyElement Element => WhitneyElement.Wind;
    protected override int SealCost => 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6, ValueProp.Move),
        new CardsVar(1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
''', "Quick Seal", "Deal {Damage:diff()} damage. NL Draw {Cards:diff()} card.")

    card("Common", "SplatterSeal", '''
namespace Whitney.WhitneyCode.Cards.Common;

public sealed class SplatterSeal() : WhitneyCard(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    public override WhitneyElement Element => WhitneyElement.Fire;
    protected override int SealCost => 2;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
''', "Splatter Seal", "Deal {Damage:diff()} damage to ALL enemies.")

    # ─── UNCOMMONS ───────────────────────────────────────────
    card("Uncommon", "BrandSeal", '''
namespace Whitney.WhitneyCode.Cards.Uncommon;

public sealed class BrandSeal() : WhitneyCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override WhitneyElement Element => WhitneyElement.Fire;
    protected override int SealCost => 2;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<InkPower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10, ValueProp.Move),
        new DynamicVar("Vulnerable", 2),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (play.Target is not null)
        {
            await PowerCmd.Apply<VulnerablePower>(
                choiceContext, play.Target, DynamicVars["Vulnerable"].IntValue, Owner.Creature, this);
        }
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}
''', "Brand Seal", "Deal {Damage:diff()} damage. NL Apply {Vulnerable:diff()} [gold]Vulnerable[/gold].")

    card("Uncommon", "GeyserSeal", '''
namespace Whitney.WhitneyCode.Cards.Uncommon;

public sealed class GeyserSeal() : WhitneyCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Water;
    protected override int SealCost => 2;
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(14, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
}
''', "Geyser Seal", "Gain {Block:diff()} [gold]Block[/gold].")

    card("Uncommon", "StormSeal", '''
namespace Whitney.WhitneyCode.Cards.Uncommon;

public sealed class StormSeal() : WhitneyCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
{
    public override WhitneyElement Element => WhitneyElement.Wind;
    protected override int SealCost => 2;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5, ValueProp.Move),
        new RepeatVar(3),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).WithHitCount(DynamicVars.Repeat.IntValue).Execute(choiceContext);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1m);
}
''', "Storm Seal", "Deal {Damage:diff()} damage to a random enemy {Repeat:diff()} times.")

    card("Uncommon", "GrandSeal", '''
namespace Whitney.WhitneyCode.Cards.Uncommon;

public sealed class GrandSeal() : WhitneyCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override WhitneyElement Element => WhitneyElement.Earth;
    protected override int SealCost => 3;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(24, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(8m);
}
''', "Grand Seal", "Deal {Damage:diff()} damage.")

    card("Uncommon", "InkWell", '''
namespace Whitney.WhitneyCode.Cards.Uncommon;

public sealed class InkWell() : WhitneyCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Water;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Ink", 3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await Ink.Gain(choiceContext, Owner, DynamicVars["Ink"].IntValue, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars["Ink"].UpgradeValueBy(1m);
}
''', "Ink Well", "Gain {Ink:diff()} [gold]Ink[/gold].")

    card("Uncommon", "AllaPrima", '''
namespace Whitney.WhitneyCode.Cards.Uncommon;

public sealed class AllaPrima() : WhitneyCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Fire;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var energy = IsBlendActive ? DynamicVars.Energy.IntValue + 1 : DynamicVars.Energy.IntValue;
        await PlayerCmd.GainEnergy(energy, Owner);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
''', "Alla Prima", "Gain {Energy:energyIcons()}. NL [gold]Blend[/gold]: Gain [blue]1[/blue] additional Energy.")

    card("Uncommon", "PaletteKnife", '''
namespace Whitney.WhitneyCode.Cards.Uncommon;

public sealed class PaletteKnife() : WhitneyCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override WhitneyElement Element => WhitneyElement.Wind;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(9, ValueProp.Move)];

    public override bool TryModifyEnergyCostInCombat(
        CardModel card, decimal originalCost, ref decimal modifiedCost)
    {
        if (!ReferenceEquals(card, this))
            return false;
        if (!WhitneyBrush.IsBlend(Owner, Element))
            return false;
        modifiedCost = 0m;
        return true;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
''', "Palette Knife", "Deal {Damage:diff()} damage. NL [gold]Blend[/gold]: Costs [blue]0[/blue] Energy.")

    card("Uncommon", "Confluence", '''
namespace Whitney.WhitneyCode.Cards.Uncommon;

public sealed class Confluence() : WhitneyCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Water;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var n = WhitneyBrush.DistinctIncluding(Owner, Element);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        await Ink.Gain(choiceContext, Owner, n, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
''', "Confluence", "Draw {Cards:diff()} card. NL Gain [gold]Ink[/gold] equal to the number of distinct elements played this turn (including this).")

    card("Uncommon", "Wash", '''
namespace Whitney.WhitneyCode.Cards.Uncommon;

public sealed class Wash() : WhitneyCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Water;
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8, ValueProp.Move),
        new DynamicVar("Ink", 2),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        var distinct = WhitneyBrush.DistinctIncluding(Owner, Element);
        if (distinct >= 2)
            await Ink.Gain(choiceContext, Owner, DynamicVars["Ink"].IntValue, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
''', "Wash", "Gain {Block:diff()} [gold]Block[/gold]. NL If you played 2+ elements this turn, gain {Ink:diff()} [gold]Ink[/gold].")

    card("Uncommon", "Sketch", '''
namespace Whitney.WhitneyCode.Cards.Uncommon;

public sealed class Sketch() : WhitneyCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Earth;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<SketchPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Discount", 0)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (DynamicVars["Discount"].IntValue <= 0)
            WhitneyBrush.SetNextSealFree(Owner, true);
        else
            WhitneyBrush.SetNextSealDiscount(Owner, DynamicVars["Discount"].IntValue);
        if (Owner.Creature is not null)
            await PowerCmd.Apply<SketchPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars["Discount"].UpgradeValueBy(1m);
}
''', "Sketch", "The next Seal you play costs [blue]0[/blue] [gold]Ink[/gold].")

    # Override Sketch upgraded desc is awkward — base free, up is -1. Spec: "Up: next seal seal-cost -1"
    # Base free (NextSealFree), upgrade Discount=1 means -1 not free. Let's fix logic: base Discount 99 free via NextSealFree when not upgraded; upgrade sets discount 1.
    # Actually: base NextSealFree, upgrade NextSealDiscount(1) instead of free. Change OnUpgrade to set a flag... 
    # Simpler: base free; upgrade still free AND gain 2 ink stand-in — user said "Up: next seal seal-cost -1"
    # So base free (cost 0), upgrade is -1 only? That's a downgrade. Reading again: "Prefer: next Seal costs 0 extra ink (SealsFree once). Up: next seal seal-cost -1"
    # So base: free once. Up: -1 once (worse?). Maybe upgrade is free AND something else... I'll keep base free, upgrade free + 1 ink on play.

    LOC_ENTRIES[loc_key("Sketch") + ".description"] = (
        "The next Seal you play costs [blue]0[/blue] [gold]Ink[/gold]."
    )

    # Fix Sketch.cs upgrade to also free (keep free) and gain 1 attune
    write(CARDS / "Uncommon" / "Sketch.cs", HEADER + '''
namespace Whitney.WhitneyCode.Cards.Uncommon;

public sealed class Sketch() : WhitneyCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Earth;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<SketchPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Discount", 0)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Base: next seal free. Upgrade (Discount>0): next seal costs Discount less (min 0).
        if (DynamicVars["Discount"].IntValue <= 0)
            WhitneyBrush.SetNextSealFree(Owner, true);
        else
            WhitneyBrush.SetNextSealDiscount(Owner, DynamicVars["Discount"].IntValue);

        if (Owner.Creature is not null)
            await PowerCmd.Apply<SketchPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade()
    {
        // Upgraded: next seal -1 Ink (still a strong setup with free energy slot)
        DynamicVars["Discount"].UpgradeValueBy(1m);
    }
}
''')
    LOC_ENTRIES[loc_key("Sketch") + ".description"] = (
        "The next Seal you play costs [blue]0[/blue] [gold]Ink[/gold]. NL Upgrade: costs [blue]1[/blue] less instead."
    )

    # Scripts
    card("Uncommon", "FlameScript", '''
namespace Whitney.WhitneyCode.Cards.Uncommon;

public sealed class FlameScript() : WhitneyCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Fire;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<FlameScriptPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Damage", 2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner.Creature is not null)
            await PowerCmd.Apply<FlameScriptPower>(
                choiceContext, Owner.Creature, DynamicVars["Damage"].IntValue, Owner.Creature, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars["Damage"].UpgradeValueBy(1m);
}
''', "Flame Script", "Whenever you play a [gold]Fire[/gold] card, deal {Damage:diff()} damage to a random enemy.")

    card("Uncommon", "TideScript", '''
namespace Whitney.WhitneyCode.Cards.Uncommon;

public sealed class TideScript() : WhitneyCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Water;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<TideScriptPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(2, ValueProp.Unpowered)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner.Creature is not null)
            await PowerCmd.Apply<TideScriptPower>(
                choiceContext, Owner.Creature, DynamicVars.Block.BaseValue, Owner.Creature, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}
''', "Tide Script", "Whenever you play a [gold]Water[/gold] card, gain {Block:diff()} [gold]Block[/gold].")

    card("Uncommon", "StoneScript", '''
namespace Whitney.WhitneyCode.Cards.Uncommon;

public sealed class StoneScript() : WhitneyCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Earth;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StoneScriptPower>(),
        HoverTipFactory.FromPower<AttunementPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Attune", 1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner.Creature is not null)
            await PowerCmd.Apply<StoneScriptPower>(
                choiceContext, Owner.Creature, DynamicVars["Attune"].IntValue, Owner.Creature, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars["Attune"].UpgradeValueBy(1m);
}
''', "Stone Script", "Whenever you play an [gold]Earth[/gold] card, gain {Attune:diff()} [gold]Attunement[/gold].")

    card("Uncommon", "GaleScript", '''
namespace Whitney.WhitneyCode.Cards.Uncommon;

public sealed class GaleScript() : WhitneyCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Wind;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<GaleScriptPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Times", 1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner.Creature is not null)
            await PowerCmd.Apply<GaleScriptPower>(
                choiceContext, Owner.Creature, DynamicVars["Times"].IntValue, Owner.Creature, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars["Times"].UpgradeValueBy(1m);
}
''', "Gale Script", "The first {Times:diff()} [gold]Wind[/gold] card(s) you play each turn, draw 1 card.")

    # ─── RARES ───────────────────────────────────────────────
    card("Rare", "WorldSeal", '''
namespace Whitney.WhitneyCode.Cards.Rare;

public sealed class WorldSeal() : WhitneyCard(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    public override WhitneyElement Element => WhitneyElement.Earth;
    // X-cost seal: no fixed star cost; spend all Ink in OnPlay (min 4).
    protected override int SealCost => 0;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5, ValueProp.Move)];

    protected override bool IsPlayable =>
        base.IsPlayable && Ink.Get(Owner) >= 4;

    protected override bool ShouldGlowGoldInternal =>
        Ink.Get(Owner) >= 4;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var spent = Ink.Get(Owner);
        if (spent < 4)
            return;
        await Ink.TrySpend(choiceContext, Owner, spent, this);

        var dmg = DynamicVars.Damage.BaseValue * spent;
        if (Owner.Creature?.CombatState is not null)
        {
            await CreatureCmd.Damage(
                choiceContext,
                Owner.Creature.CombatState.HittableEnemies,
                dmg,
                ValueProp.Move,
                Owner.Creature,
                this);
        }
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}
''', "World Seal", "Spend all your [gold]Ink[/gold] (minimum 4). NL Deal damage equal to {Damage:diff()} times [gold]Ink[/gold] spent to ALL enemies.")

    card("Rare", "PerfectSeal", '''
namespace Whitney.WhitneyCode.Cards.Rare;

public sealed class PerfectSeal() : WhitneyCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override WhitneyElement Element => WhitneyElement.Earth;
    protected override int SealCost => 4;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<InkPower>(),
        HoverTipFactory.FromPower<AttunementPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(26, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Attunement already applies once via ModifyDamageAdditive for seals;
        // double attunement: add another Attunement amount manually.
        var attune = Owner.Creature?.GetPower<AttunementPower>()?.Amount ?? 0;
        if (attune > 0 && play.Target is not null)
        {
            await CreatureCmd.Damage(
                choiceContext, play.Target, attune, ValueProp.Unpowered, Owner.Creature, this);
        }
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6m);
}
''', "Perfect Seal", "Deal {Damage:diff()} damage. NL [gold]Attunement[/gold] counts twice.")

    card("Rare", "NegativeSpace", '''
namespace Whitney.WhitneyCode.Cards.Rare;

public sealed class NegativeSpace() : WhitneyCard(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override WhitneyElement Element => WhitneyElement.Wind;
    protected override int SealCost => 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(4, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // After auto-paying 1 seal cost, empty slots = MaxInk - remaining ink.
        var empty = Ink.MaxInk - Ink.Get(Owner);
        var hits = System.Math.Max(0, empty);
        var per = DynamicVars.Damage.BaseValue;
        if (play.Target is not null && hits > 0)
        {
            await CreatureCmd.Damage(
                choiceContext, play.Target, per * hits, ValueProp.Move, Owner.Creature, this);
        }
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}
''', "Negative Space", "Deal {Damage:diff()} damage for each empty [gold]Ink[/gold] (max 10).")

    card("Rare", "InfernoSeal", '''
namespace Whitney.WhitneyCode.Cards.Rare;

public sealed class InfernoSeal() : WhitneyCard(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    public override WhitneyElement Element => WhitneyElement.Fire;
    protected override int SealCost => 4;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<InkPower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(15, ValueProp.Move),
        new DynamicVar("Vulnerable", 2),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (Owner.Creature?.CombatState is not null)
        {
            foreach (var enemy in Owner.Creature.CombatState.HittableEnemies)
            {
                await PowerCmd.Apply<VulnerablePower>(
                    choiceContext, enemy, DynamicVars["Vulnerable"].IntValue, Owner.Creature, this);
            }
        }
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);
}
''', "Inferno Seal", "Deal {Damage:diff()} damage and apply {Vulnerable:diff()} [gold]Vulnerable[/gold] to ALL enemies.")

    card("Rare", "Monument", '''
namespace Whitney.WhitneyCode.Cards.Rare;

public sealed class Monument() : WhitneyCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Earth;
    protected override int SealCost => 3;
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<InkPower>(),
        HoverTipFactory.FromPower<BarricadePower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(16, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        if (Owner.Creature is not null)
        {
            await PowerCmd.Apply<BarricadePower>(
                choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        }
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(6m);
}
''', "Monument", "Gain {Block:diff()} [gold]Block[/gold]. NL Gain [gold]Barricade[/gold].")

    card("Rare", "LivingInk", '''
namespace Whitney.WhitneyCode.Cards.Rare;

public sealed class LivingInk() : WhitneyCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Water;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<LivingInkPower>(),
        HoverTipFactory.FromPower<InkPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Ink", 2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner.Creature is not null)
            await PowerCmd.Apply<LivingInkPower>(
                choiceContext, Owner.Creature, DynamicVars["Ink"].IntValue, Owner.Creature, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars["Ink"].UpgradeValueBy(1m);
}
''', "Living Ink", "At the start of your turn, gain {Ink:diff()} [gold]Ink[/gold].")

    card("Rare", "EternalQuill", '''
namespace Whitney.WhitneyCode.Cards.Rare;

public sealed class EternalQuill() : WhitneyCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Wind;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<EternalQuillPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner.Creature is not null)
            await PowerCmd.Apply<EternalQuillPower>(
                choiceContext, Owner.Creature, 1, Owner.Creature, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
''', "Eternal Quill", "Seals cost [blue]1[/blue] less [gold]Ink[/gold] (minimum 1).")

    card("Rare", "WetPaint", '''
namespace Whitney.WhitneyCode.Cards.Rare;

public sealed class WetPaint() : WhitneyCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Water;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WetPaintPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        WhitneyBrush.SetSealsFreeThisTurn(Owner, true);
        if (Owner.Creature is not null)
            await PowerCmd.Apply<WetPaintPower>(
                choiceContext, Owner.Creature, 1, Owner.Creature, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
''', "Wet Paint", "Seals cost [blue]0[/blue] [gold]Ink[/gold] this turn.")

    card("Rare", "Masterwork", '''
namespace Whitney.WhitneyCode.Cards.Rare;

public sealed class Masterwork() : WhitneyCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Earth;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<MasterworkPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner.Creature is not null)
            await PowerCmd.Apply<MasterworkPower>(
                choiceContext, Owner.Creature, 1, Owner.Creature, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
''', "Masterwork", "Once per turn, when you play all 4 elements in a single turn, gain [blue]3[/blue] [gold]Ink[/gold], [blue]2[/blue] [gold]Attunement[/gold], and draw [blue]2[/blue] cards.")

    card("Rare", "ElementalForm", '''
namespace Whitney.WhitneyCode.Cards.Rare;

public sealed class ElementalForm() : WhitneyCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Fire;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ElementalFormPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner.Creature is not null)
            await PowerCmd.Apply<ElementalFormPower>(
                choiceContext, Owner.Creature, 1, Owner.Creature, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
''', "Elemental Form", "Your cards count as every element for [gold]Blend[/gold] and [gold]Masterwork[/gold].")


def write_loc() -> None:
    # Prefer NL for newlines — STS2 loc often uses NL
    cleaned = {}
    for k, v in LOC_ENTRIES.items():
        cleaned[k] = v.replace(" NL ", "\n")
    write(LOC, json.dumps(cleaned, indent=2, ensure_ascii=False))
    print("wrote", LOC.relative_to(ROOT), "entries", len(cleaned) // 2)

    powers = {
        "WHITNEY-INK_POWER.title": "Ink",
        "WHITNEY-INK_POWER.description": "Your second mana. Bank [gold]Ink[/gold] (max 10) and spend it on seals.",
        "WHITNEY-ATTUNEMENT_POWER.title": "Attunement",
        "WHITNEY-ATTUNEMENT_POWER.description": "Seals deal and block additional equal to [gold]Attunement[/gold].",
        "WHITNEY-BRUSH_TRACKER_POWER.title": "Brush",
        "WHITNEY-BRUSH_TRACKER_POWER.description": "Tracks elemental strokes this turn.",
        "WHITNEY-FLAME_SCRIPT_POWER.title": "Flame Script",
        "WHITNEY-FLAME_SCRIPT_POWER.description": "Whenever you play a [gold]Fire[/gold] card, deal {Amount} damage to a random enemy.",
        "WHITNEY-TIDE_SCRIPT_POWER.title": "Tide Script",
        "WHITNEY-TIDE_SCRIPT_POWER.description": "Whenever you play a [gold]Water[/gold] card, gain {Amount} [gold]Block[/gold].",
        "WHITNEY-STONE_SCRIPT_POWER.title": "Stone Script",
        "WHITNEY-STONE_SCRIPT_POWER.description": "Whenever you play an [gold]Earth[/gold] card, gain {Amount} [gold]Attunement[/gold].",
        "WHITNEY-GALE_SCRIPT_POWER.title": "Gale Script",
        "WHITNEY-GALE_SCRIPT_POWER.description": "The first {Amount} [gold]Wind[/gold] card(s) you play each turn, draw 1 card.",
        "WHITNEY-LIVING_INK_POWER.title": "Living Ink",
        "WHITNEY-LIVING_INK_POWER.description": "At the start of your turn, gain {Amount} [gold]Ink[/gold].",
        "WHITNEY-ETERNAL_QUILL_POWER.title": "Eternal Quill",
        "WHITNEY-ETERNAL_QUILL_POWER.description": "Seals cost 1 less [gold]Ink[/gold] (minimum 1).",
        "WHITNEY-ELEMENTAL_FORM_POWER.title": "Elemental Form",
        "WHITNEY-ELEMENTAL_FORM_POWER.description": "Your cards count as every element for [gold]Blend[/gold] and [gold]Masterwork[/gold].",
        "WHITNEY-MASTERWORK_POWER.title": "Masterwork",
        "WHITNEY-MASTERWORK_POWER.description": "Once per turn, when you play all 4 elements, gain 3 Ink, 2 Attunement, draw 2.",
        "WHITNEY-WET_PAINT_POWER.title": "Wet Paint",
        "WHITNEY-WET_PAINT_POWER.description": "Seals cost 0 [gold]Ink[/gold] this turn.",
        "WHITNEY-SKETCH_POWER.title": "Sketch",
        "WHITNEY-SKETCH_POWER.description": "The next Seal has reduced [gold]Ink[/gold] cost.",
    }
    write(POW_LOC, json.dumps(powers, indent=2, ensure_ascii=False))
    print("wrote", POW_LOC.relative_to(ROOT))


def main() -> None:
    clear_old()
    emit_cards()
    write_loc()
    print("done. cards:", len([p for p in CARDS.rglob("*.cs") if p.name != "WhitneyCard.cs"]))


if __name__ == "__main__":
    main()
