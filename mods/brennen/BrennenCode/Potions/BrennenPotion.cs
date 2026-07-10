using BaseLib.Abstracts;
using BaseLib.Utils;
using Brennen.BrennenCode.Character;

namespace Brennen.BrennenCode.Potions;

[Pool(typeof(BrennenPotionPool))]
public abstract class BrennenPotion : CustomPotionModel;