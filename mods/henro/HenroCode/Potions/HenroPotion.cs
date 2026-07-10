using BaseLib.Abstracts;
using BaseLib.Utils;
using Henro.HenroCode.Character;

namespace Henro.HenroCode.Potions;

[Pool(typeof(HenroPotionPool))]
public abstract class HenroPotion : CustomPotionModel;