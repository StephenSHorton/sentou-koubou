using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using Henro.HenroCode.Cards;
using Henro.HenroCode.Extensions;
using Henro.HenroCode.Relics;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace Henro.HenroCode.Character;

/// <summary>
/// The Pilgrim (遍路) — vertical-slice playable character for sentou-koubou.
/// Uses PlaceholderCharacterModel so combat/rest/merchant visuals fall back to Ironclad
/// until custom Spine/scenes are ready.
/// </summary>
public class Henro : PlaceholderCharacterModel
{
    public const string CharacterId = "Henro";

    /// <summary>Soft indigo — pilgrimage / dusk road.</summary>
    public static readonly Color Color = new("6b7fd7");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 72;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeHenro>(),
        ModelDb.Card<StrikeHenro>(),
        ModelDb.Card<StrikeHenro>(),
        ModelDb.Card<StrikeHenro>(),
        ModelDb.Card<StrikeHenro>(),
        ModelDb.Card<DefendHenro>(),
        ModelDb.Card<DefendHenro>(),
        ModelDb.Card<DefendHenro>(),
        ModelDb.Card<DefendHenro>(),
        ModelDb.Card<DefendHenro>(),
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<PilgrimBeads>(),
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<HenroCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<HenroRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<HenroPotionPool>();

    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }

    public override string CustomIconTexturePath => "character_icon_henro.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_henro.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_henro_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_henro.png".CharacterUiPath();
}
