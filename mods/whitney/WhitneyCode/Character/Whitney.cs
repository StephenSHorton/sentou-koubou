using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Whitney.WhitneyCode.Cards.Basic;
using Whitney.WhitneyCode.Extensions;
using Whitney.WhitneyCode.Relics;

namespace Whitney.WhitneyCode.Character;

/// <summary>
/// Whitney — atelier witch. Energy + Ink dual mana, four elements, dual-purpose seals.
/// Starter teaches gen (Channel/Novice) and spend (Apprentice Seal). Attunement scales all attacks.
/// Family pack character #2 for sentou-koubou.
/// </summary>
public class Whitney : PlaceholderCharacterModel
{
    public const string CharacterId = "Whitney";

    /// <summary>Indigo ink-mage — matches D3 dress / hat clothing lock.</summary>
    public static readonly Color Color = new("4B3F8C");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 74;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<Spark>(),
        ModelDb.Card<Spark>(),
        ModelDb.Card<Spark>(),
        ModelDb.Card<Spark>(),
        ModelDb.Card<Ripple>(),
        ModelDb.Card<Ripple>(),
        ModelDb.Card<Ripple>(),
        ModelDb.Card<ChannelInk>(),
        ModelDb.Card<NoviceSeal>(),
        ModelDb.Card<ApprenticeSeal>(),
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<TravelersInkpot>(),
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

    /// <summary>
    /// Full-screen character select backdrop (otherwise BaseLib reuses Ironclad).
    /// Scene is injected into the .pck after pack — PckPacker cannot ship .tscn.
    /// </summary>
    public override string CustomCharacterSelectBg =>
        "res://scenes/screens/char_select/char_select_bg_whitney.tscn";

    /// <summary>
    /// Combat body: Blender flipbook (idle / attack / hit / dead) instead of Ironclad.
    /// See tools/char-anim-pipeline/export/whitney/ and BLENDER_PIPELINE.md.
    /// </summary>
    public override NCreatureVisuals? CreateCustomVisuals() => WhitneyCombatVisuals.Create();

    /// <summary>Dead clip is 24 frames @ 24fps = 1.0s; small headroom.</summary>
    public override float DeathAnimTime => 1.1f;
}
