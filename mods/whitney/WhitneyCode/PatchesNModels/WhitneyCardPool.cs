using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;

namespace Whitney.WhitneyCode.PatchesNModels;

public class WhitneyCardPool : CustomCardPoolModel
{
    private const string FramePathAttack = "res://Whitney/images/ui/bg_attack_WTN.png";
    private const string FramePathPower = "res://Whitney/images/ui/bg_power_WTN.png";
    private const string FramePathSkill = "res://Whitney/images/ui/bg_skill_WTN.png";

    // 卡池的ID。必须唯一防撞车。
    public override string Title => "Whitney";

    //public override string EnergyColorName => "defect";//"whitney";

    // 卡池的主题色。通常是卡牌框架的颜色。
    public override Color DeckEntryCardColor => new("4B3F8C");

    public override Color EnergyOutlineColor => new("4B3F8C");

    // 卡池是否是无色。例如事件、状态等卡池就是无色的。
    public override bool IsColorless => false;

    public override bool SeenByDefault => true;

    public override string BigEnergyIconPath => "res://Whitney/images/charui/big_energy.png";

    public override string TextEnergyIconPath => "res://Whitney/images/charui/text_energy.png";

    // public override float H => 0.72f;
    // public override float S => 0.48f;
    // public override float V => 0.78f;
    public override float H => 0.72f;
    public override float S => 0.48f;
    public override float V => 0.78f;

    public override Texture2D CustomFrame(CustomCardModel card)
    {
        var path = card.Type switch
        {
            CardType.Attack => FramePathAttack,
            CardType.Power => FramePathPower,
            _ => FramePathSkill
        };
        return PreloadManager.Cache.GetTexture2D(path);
    }

    // Marisa used a custom banner shader under Materials/; that path is not in the quick .pck yet.
    // HSV shell (H/S/V above) + custom frame textures are enough until MegaDot export ships shaders.
}