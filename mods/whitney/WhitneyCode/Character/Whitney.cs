using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using Whitney.WhitneyCode.Cards.Basic;
using Whitney.WhitneyCode.Extensions;
using Whitney.WhitneyCode.Relics;

namespace Whitney.WhitneyCode.Character;

/// <summary>
/// Whitney — atelier witch. Energy + Ink dual mana, four elements, dual-purpose seals.
/// Family pack character #2 for sentou-koubou.
/// </summary>
public class Whitney : PlaceholderCharacterModel
{
    public const string CharacterId = "Whitney";

    /// <summary>Soft sage — Witch Hat Atelier parchment &amp; leaf.</summary>
    public static readonly Color Color = new("7a9e8a");

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
        ModelDb.Card<Ripple>(),
        ModelDb.Card<ChannelInk>(),
        ModelDb.Card<NoviceSeal>(),
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
    /// Full-screen character select backdrop (otherwise vanilla Ironclad scene is reused).
    /// </summary>
    public override string CustomCharacterSelectBg =>
        "res://scenes/screens/char_select/char_select_bg_whitney.tscn";
}
