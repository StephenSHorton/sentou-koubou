using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Brennen.BrennenCode.Powers;

/// <summary>First lethal hit: heal to 25% max HP instead, then remove.</summary>
public sealed class GuardianAngelPower : BrennenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Guardian Angel",
            "The first time you would die, heal to [blue]25%[/blue] of your max HP instead.",
            "The first time you would die, heal to [blue]25%[/blue] of your max HP instead.");

    public override bool ShouldDie(Creature creature)
    {
        if (creature != Owner)
            return true;
        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner)
            return;
        Flash();
        var targetHp = System.Math.Max(1, creature.MaxHp / 4);
        var heal = targetHp - creature.CurrentHp;
        if (heal > 0)
            await CreatureCmd.Heal(creature, heal);
        await PowerCmd.Remove(this);
    }
}
