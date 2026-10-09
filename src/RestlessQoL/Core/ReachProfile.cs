using System;

namespace RestlessQoL.Core;

public readonly struct ReachProfile
{
    public readonly string Name;
    public readonly float Shared, Repair, Seal, Harvest;
    private ReachProfile(string name, float shared, float repair, float seal, float harvest)
    { Name = name; Shared = shared; Repair = repair; Seal = seal; Harvest = harvest; }

    public static ReachProfile At(int index)
    {
        switch (Math.Max(0, Math.Min(4, index)))
        {
            case 0: return new ReachProfile("Small", 15f, 8f, 8f, 2.5f);
            case 1: return new ReachProfile("Standard", 25f, 15f, 15f, 4f);
            case 2: return new ReachProfile("Far", 50f, 25f, 25f, 6f);
            case 3: return new ReachProfile("Wide", 80f, 35f, 35f, 7f);
            default: return new ReachProfile("Maximum", 120f, 40f, 40f, 8f);
        }
    }
}
