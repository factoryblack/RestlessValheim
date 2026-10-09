namespace RestlessQoL.Core;

/// <summary>Resolve reach at use time. Custom entries are never overwritten by a profile.</summary>
public static class ReachPolicy
{
    public static bool Global => ModConfig.GlobalReachEnabled != null && ModConfig.GlobalReachEnabled.Value;
    public static ReachProfile Profile => ReachProfile.At(ModConfig.GlobalReachProfile != null ? ModConfig.GlobalReachProfile.Value : 2);
    public static float ResourceReach => Global ? Profile.Shared : ModConfig.StorageRange.Value;
    public static float StationReach => Global ? Profile.Shared : ModConfig.WorkbenchRange.Value;
    public static float RepairReach => Global ? Profile.Repair : ModConfig.AreaRepairRadius.Value;
    public static float SealReach => Global ? Profile.Seal : ModConfig.AreaSealRadius.Value;
    public static float HarvestReach(float custom) => Global ? Profile.Harvest : custom;
}
