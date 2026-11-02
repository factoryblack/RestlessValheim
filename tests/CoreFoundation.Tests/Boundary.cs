using System.Reflection;

public sealed class Player { public void Load() { } }
namespace RestlessQoL.HudTweaks
{
    internal static class ExtraSlots
    {
        internal static Player? Before, After;
        internal static void BeginRecovery(Player p) => Before = p;
        internal static void FinishRecovery(Player p) => After = p;
    }
}

namespace RestlessQoL
{
    internal static class Plugin { internal static Log Log = new(); }
    internal sealed class Log
    {
        public void LogInfo(string text) { }
        public void LogError(string text) => throw new Exception(text);
    }
}
namespace HarmonyLib
{
    // Only the Harmony/native boundary is substituted. Production FeatureModule
    // performs its real reflection, startup decision and lifecycle dispatch.
    public class Harmony(string id)
    {
        public static int Created, Patched;
        private readonly string _id = Register(id);
        private static string Register(string id) { Created++; return id; }
        public void Patch(MethodBase original, HarmonyMethod? prefix, HarmonyMethod? postfix,
            HarmonyMethod? transpiler, HarmonyMethod? finalizer, object? ilmanipulator) => Patched++;
    }
    public class HarmonyAttribute : Attribute { public HarmonyMethod info = new(); }
    public sealed class HarmonyPrefix : HarmonyAttribute { }
    public sealed class HarmonyPostfix : HarmonyAttribute { }
    public sealed class HarmonyTranspiler : HarmonyAttribute { }
    public sealed class HarmonyFinalizer : HarmonyAttribute { }
    public sealed class HarmonyPatch : HarmonyAttribute
    {
        public HarmonyPatch() { }
        public HarmonyPatch(Type type, string name) { info.declaringType = type; info.methodName = name; }
    }
    public class HarmonyMethod
    {
        public Type declaringType = typeof(object);
        public string methodName = "ToString";
        public Type[]? argumentTypes;
        public HarmonyMethod() { }
        public HarmonyMethod(MethodInfo method) { }
        public static HarmonyMethod Merge(List<HarmonyMethod> methods) => methods.Last();
    }
    public static class AccessTools
    {
        public static MethodInfo? DeclaredMethod(Type type, string name, Type[]? args = null) => type.GetMethod(name, args ?? Type.EmptyTypes);
        public static MethodInfo? Method(Type type, string name, Type[]? args = null) => DeclaredMethod(type, name, args);
    }
}
