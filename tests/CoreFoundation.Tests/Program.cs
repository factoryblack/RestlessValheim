using RestlessQoL.Core;
using HarmonyLib;

var checks = 0;
void Check(bool ok, string why) { checks++; if (!ok) throw new Exception(why); }
foreach (var configured in new[] { false, true })
foreach (var foundation in new[] { false, true })
foreach (var enabled in new[] { false, true })
{
    CoreFeatures.Configure(configured);
    Harmony.Created = Harmony.Patched = 0;
    var module = new Probe(foundation, enabled);
    module.TickConfigured();
    Check(module.Ticks == (configured ? 1 : 0), "Foundation mode cannot tick unloaded modules; normal mode preserves existing live feature toggles");
    module.TryLoad();
    var expected = enabled && (foundation || configured);
    Check(module.IsLoaded == expected, "The startup policy must gate both optional and explicitly disabled modules");
    Check(module.PatchReads == (expected ? 1 : 0), "Suppressed modules must not even evaluate patch types");
    Check(Harmony.Created == (expected ? 1 : 0), "Suppressed modules must not acquire a Harmony instance");
    Check(Harmony.Patched == (expected ? 1 : 0), "Suppressed modules must not install native patches");
    Check(module.Loads == (expected ? 1 : 0), "Suppressed modules must not subscribe events or mutate prefabs through OnLoaded");
    module.TickConfigured();
    Check(module.Ticks == (configured ? 2 : expected ? 1 : 0), "Suppressed optional module Tick must not create visuals or change gameplay");
    module.TryLoad();
    Check(module.Loads == (expected ? 1 : 0), "Repeated load cannot duplicate hooks or subscriptions");
}

CoreFeatures.Configure(false);
Harmony.Created = Harmony.Patched = 0;
var recovery = new RestlessQoL.HudTweaks.InventoryRecovery();
recovery.TryLoad();
Check(recovery.IsLoaded && Harmony.Patched == 2, "Foundation recovery installs exactly the two character-load hooks");
var player = new Player();
var hooks = typeof(RestlessQoL.HudTweaks.InventoryRecovery).GetNestedType("Patches", System.Reflection.BindingFlags.NonPublic)!;
hooks.GetMethod("BeforeLoad", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.Invoke(null, [player]);
hooks.GetMethod("AfterLoad", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.Invoke(null, [player]);
Check(RestlessQoL.HudTweaks.ExtraSlots.Before == player && RestlessQoL.HudTweaks.ExtraSlots.After == player,
    "Both recovery hooks delegate the same character to the existing item migration boundary");
CoreFeatures.Configure(true);
var ordinaryRecovery = new RestlessQoL.HudTweaks.InventoryRecovery();
ordinaryRecovery.TryLoad();
Check(!ordinaryRecovery.IsLoaded && Harmony.Patched == 2, "Normal mode must retain existing ExtraSlots handling without duplicate recovery hooks");

var root = new DirectoryInfo(AppContext.BaseDirectory);
while (root != null && !File.Exists(Path.Combine(root.FullName, "versions.yaml"))) root = root.Parent;
if (root == null) throw new Exception("Cannot locate repository");
string Read(string p) => File.ReadAllText(Path.Combine(root.FullName, p));
var core = "src/RestlessQoL/";
var plugin = Read(core + "Plugin.cs");
Check(!plugin.Contains("module.Tick();"), "All plugin update branches must use the loaded-module gate");
Check(plugin.Split("module.TickConfigured();").Length == 3, "Both normal and settings-open tick branches must use the gate");
Check(plugin.IndexOf("ModConfig.Bind(Config)") < plugin.IndexOf("module.TryLoad()"), "Startup mode must resolve before modules load");
var files = Directory.GetFiles(Path.Combine(root.FullName, core), "*.cs", SearchOption.AllDirectories);
var foundations = files.Where(f => File.ReadAllText(f).Contains("override bool IsFoundation => true"))
    .Select(Path.GetFileNameWithoutExtension).OrderBy(x => x).ToArray();
Check(string.Join(",", foundations) == "InventoryRecovery,Ledger,SettingsUi,StorageSync", "New optional modules cannot silently become shared infrastructure");
var settings = Read(core + "Core/SettingsUi.cs");
Check(settings.Contains("CoreFeatures.OptionalEnabled ? base.PatchTypes : Type.EmptyTypes"), "Foundation settings must not patch the native pause menu");
var config = Read(core + "Core/ModConfig.cs");
var debug = config.Substring(config.IndexOf("internal static void ApplyJotunnDebug()"));
Check(debug.IndexOf("if (!CoreFeatures.OptionalEnabled)") < debug.IndexOf("SetJotunn("), "Foundation mode must not rewrite Jotunn config");
Check(config.Contains("CoreFeatures.Configure(OptionalFeatures.Value)"), "Persisted config must feed the startup policy");
Check(Read(core + "Hud/InventoryRecovery.cs").Contains("!CoreFeatures.OptionalEnabled"), "Inventory migration only belongs in foundation mode");
Check(Read(core + "Hud/SlotLock.cs").Contains("CoreFeatures.OptionalEnabled && ModConfig.SlotLockEnabled.Value"), "Addon calls cannot reactivate Core slot locking");
Check(Read(core + "Core/ReachPolicy.cs").Contains("CoreFeatures.OptionalEnabled &&"), "Addon calls cannot inherit a disabled Core global-reach profile");
Check(Read("versions.yaml").Split("  drawers:")[1].Split("  storage:")[0].Contains("needs_core: true"), "Drawers package must require the shared foundation");
Check(Read("src/RestlessDrawers/Plugin.cs").Contains("BepInDependency(\"restless.core\""), "Drawers loader must require Core");
Check(Read("src/RestlessDrawers/RestlessDrawers.csproj").Contains("ProjectReference Include=\"..\\RestlessQoL\\RestlessQoL.csproj\""), "Drawers must build against the shared project");
Console.WriteLine($"Core foundation lifecycle/configuration: {checks} checks passed. Native/Unity side effects are substituted, not exercised in-game.");

sealed class Probe(bool foundation, bool enabled) : FeatureModule
{
    public int PatchReads, Loads, Ticks;
    public override string Id => "probe";
    public override bool IsFoundation => foundation;
    public override bool Enabled => enabled;
    protected override Type[] PatchTypes { get { PatchReads++; return [typeof(Patches)]; } }
    protected override void OnLoaded() => Loads++;
    public override void Tick() => Ticks++;
    private static class Patches
    {
        [HarmonyPrefix, HarmonyPatch]
        private static void Before() { }
    }
}
