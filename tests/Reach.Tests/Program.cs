using RestlessQoL.Core;
using RestlessQoL.Building;
using UnityEngine;

var checks = 0;
void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
var shared = new[]{15f,25f,50f,80f,120f};
var repair = new[]{8f,15f,25f,35f,40f};
var harvest = new[]{4f,6f,8f,10f,12f};
Check(!ReachPolicy.Global && ReachPolicy.ResourceReach == 50 && ReachPolicy.StationReach == 40, "Existing custom distances survive introduction");
for (var i=0;i<5;i++)
{
    ModConfig.GlobalReachProfile.Value=i; ModConfig.GlobalReachEnabled.Value=true;
    Check(ReachPolicy.ResourceReach == shared[i] && ReachPolicy.StationReach == shared[i], "Storage and station share profile " + i);
    Check(ReachPolicy.RepairReach == repair[i] && ReachPolicy.SealReach == repair[i], "Smaller repair/seal set " + i);
    Check(ReachPolicy.HarvestReach(3.25f) == harvest[i], "Smaller crop/hive set " + i);
    ModConfig.GlobalReachEnabled.Value=false;
    Check(ReachPolicy.ResourceReach == 50 && ReachPolicy.StationReach == 40 && ReachPolicy.RepairReach == 31 && ReachPolicy.SealReach == 17 && ReachPolicy.HarvestReach(3.25f)==3.25f, "Custom restore after profile " + i);
}
ModConfig.GlobalReachEnabled.Value=true; ModConfig.GlobalReachProfile.Value=4;
ModConfig.StorageRange.Value=19; ModConfig.WorkbenchRange.Value=23;
Check(ReachPolicy.ResourceReach==120 && ReachPolicy.StationReach==120, "Custom edits cannot drift active profile");
ModConfig.GlobalReachEnabled.Value=false;
Check(ReachPolicy.ResourceReach==19 && ReachPolicy.StationReach==23, "Custom values remain separate underneath");
Check(ReachProfile.At(-4).Shared==15 && ReachProfile.At(99).Shared==120, "Defensive profile bounds");
var original = new StationReachState(3,20,2);
original.Resolve(true,120,out var use,out var build,out var extra);
Check(use==120 && build==120 && extra==0, "Station circle and permission cannot gain hidden upgrade radius");
original.Resolve(true,15,out use,out build,out extra);
Check(use==15 && build==15 && extra==0, "Loaded station shrinks without retaining previous maximum");
original.Resolve(false,120,out use,out build,out extra);
Check(use==3 && build==20 && extra==2, "Disabling override restores native use/base/upgrade values");
Check(ReachGeometry.Sphere(new(120,0,0),new(),120), "Resource boundary included");
Check(!ReachGeometry.Sphere(new(120.01f,0,0),new(),120), "Collider overlap cannot extend resource boundary");
Check(!ReachGeometry.Sphere(new(0,121,0),new(),120), "Resource search respects height");
Check(ReachGeometry.Station(new(119,500,0),new(),120), "Sealing agrees with native horizontal station coverage");
Check(!ReachGeometry.Station(new(120,0,0),new(),120), "Native station boundary excluded");
Check(!ReachGeometry.Sphere(new(),new(),0) && !ReachGeometry.Station(new(),new(),-1), "Non-positive reach excludes objects");
Console.WriteLine($"Reach policy: {checks} checks passed.");
