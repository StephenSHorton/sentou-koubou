using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Patches.UI;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Whitney.WhitneyCode.Cards;
using Whitney.WhitneyCode.Extensions;
using Whitney.WhitneyCode.PatchesNModels;
using Whitney.WhitneyCode.Relics;

namespace Whitney.WhitneyCode.Character;

/// <summary>
/// Whitney — atelier witch. Kit architecture adapted from MarisaMod:
/// Amplify kickers, Inkbound enchantment (was Starlit), Saturate (was Charge-Up).
/// Combat body uses Blender flipbook, not Marisa spine.
/// </summary>
public class WhitneyCharacter : PlaceholderCharacterModel
{
    public const string CharacterId = "Whitney"; // model id still Whitney

    /// <summary>Indigo ink-mage — matches D3 dress / hat clothing lock.</summary>
    public static readonly Color Color = new("4B3F8C");

    public override Color NameColor => Color;
    public override Color MapDrawingColor => Color;
    public override Color RemoteTargetingLineColor => Color;
    public override Color EnergyLabelOutlineColor => Color;

    /// <summary>
    /// Combat mana orb behind the energy count. PlaceholderCharacterModel defaults this
    /// to Ironclad; use Whitney's violet bubble (all layers) instead.
    /// </summary>
    public override CustomEnergyCounter? CustomEnergyCounter => new(
        _ => "res://Whitney/images/charui/big_energy.png",
        outlineColor: Color,
        burstColor: new Color("8B6CFF"));

    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 75;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<SparkStrike>(),
        ModelDb.Card<SparkStrike>(),
        ModelDb.Card<SparkStrike>(),
        ModelDb.Card<SparkStrike>(),
        ModelDb.Card<DefendWhitney>(),
        ModelDb.Card<DefendWhitney>(),
        ModelDb.Card<DefendWhitney>(),
        ModelDb.Card<DefendWhitney>(),
        ModelDb.Card<MasterSpark>(),
        ModelDb.Card<UpSweep>(),
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<MiniHakkero>(),
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<WhitneyCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<WhitneyRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<WhitneyPotionPool>();

    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }

    public override string CustomIconTexturePath => "character_icon_whitney.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_whitney.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_whitney_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_whitney.png".CharacterUiPath();

    public override string CustomCharacterSelectBg =>
        "res://scenes/screens/char_select/char_select_bg_whitney.tscn";

    /// <summary>
    /// Optional rock-paper-scissors arms from Marisa UI pack (retheme later).
    /// </summary>
    public override string CustomArmPointingTexturePath => "res://Whitney/images/ui/hand_point.png";
    public override string CustomArmRockTexturePath => "res://Whitney/images/ui/hand_rock.png";
    public override string CustomArmPaperTexturePath => "res://Whitney/images/ui/hand_paper.png";
    public override string CustomArmScissorsTexturePath => "res://Whitney/images/ui/hand_scissors.png";

    public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_ironclad";

    public override RelicIconData CustomYummyCookie => new(
        "res://Whitney/images/relics/cookie_whitney.png",
        "res://Whitney/images/relics/cookie_small.png",
        "res://Whitney/images/relics/cookie_small_outline.png");

    public override List<string> GetArchitectAttackVfx() =>
    [
        "vfx/vfx_attack_blunt",
        "vfx/vfx_heavy_blunt",
        "vfx/vfx_attack_slash",
        "vfx/vfx_bloody_impact",
        "vfx/vfx_rock_shatter"
    ];

    /// <summary>Combat body: Blender flipbook (idle / attack / hit / dead).</summary>
    public override NCreatureVisuals? CreateCustomVisuals() => WhitneyCombatVisuals.Create();

    /// <summary>Dead clip is 24 frames @ 24fps = 1.0s; small headroom.</summary>
    public override float DeathAnimTime => 1.1f;
}
