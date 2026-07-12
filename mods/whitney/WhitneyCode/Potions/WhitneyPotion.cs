using BaseLib.Abstracts;
using BaseLib.Utils;
using Whitney.WhitneyCode.Character;

namespace Whitney.WhitneyCode.Potions;

[Pool(typeof(WhitneyPotionPool))]
public abstract class WhitneyPotion : CustomPotionModel;