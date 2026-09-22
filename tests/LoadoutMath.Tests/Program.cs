using RestlessQoL.Core;

static void Equal(string name, float expected, float actual)
{
    if (Math.Abs(expected - actual) > 0.0001f)
        throw new Exception($"{name}: expected {expected}, got {actual}");
    Console.WriteLine($"PASS {name}");
}
// Real regressions: a neutral damage multiplier is 1, whereas an armour
// percentage is zero at neutral. Attack and recovery stack differently.
Equal("neutral attack is not +100%", 1f, LoadoutMath.Damage(1f, 1f, 0f));
Equal("bear-style type bonus", 1.1f, LoadoutMath.Damage(1f, 1f, .1f));
Equal("two 10% effects compound to 21%", 1.21f, LoadoutMath.Damage(LoadoutMath.Damage(1f, 1f, .1f), 1f, .1f));
Equal("skill multiplier combines with slash", 1.65f, LoadoutMath.Damage(1f, 1.5f, .1f));
Equal("increase and reduction can cancel", 1f, LoadoutMath.Damage(LoadoutMath.Damage(1f, 1.25f, 0f), .8f, 0f));
Equal("neutral armour", 30f, LoadoutMath.Armor(30f, 0f, 0f));
Equal("armour flat applies before percentage", 44f, LoadoutMath.Armor(30f, 10f, .1f));
Equal("negative armour modifier", 15f, LoadoutMath.Armor(30f, 0f, -.5f));
Equal("armour effect order retained", 49f, LoadoutMath.Armor(LoadoutMath.Armor(30f, 10f, .1f), 5f, 0f));
Equal("positive recovery adds", 1.8f, LoadoutMath.Regen(LoadoutMath.Regen(1f, 1.5f), 1.3f));
Equal("recovery reductions multiply", .9f, LoadoutMath.Regen(LoadoutMath.Regen(LoadoutMath.Regen(1f, 1.5f), 1.3f), .5f));
Equal("negative residual preserved", -4f, LoadoutMath.Residual(40f, 44f));
Equal("unattributed contribution reconciles", 51f, 44f + LoadoutMath.Residual(51f, 44f));
if (!LoadoutMath.Changed(.005f)) throw new Exception("Sub-1% bonuses must remain visible");
Console.WriteLine("14 loadout arithmetic regressions passed.");
