using BaseLib.Abstracts;
using BaseLib.Utils;
using Blake.BlakeCode.Character;

namespace Blake.BlakeCode.Potions;

[Pool(typeof(BlakePotionPool))]
public abstract class BlakePotion : CustomPotionModel;
