using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace RestlessQoL.Core;

public abstract class FeatureModule
{
    public abstract string Id { get; }
    public abstract bool Enabled { get; }
    protected Harmony? Harmony { get; private set; }

    // Nested type named Patches is picked up automatically. Override only for extras.
    protected virtual Type[] PatchTypes
    {
        get
        {
            var patches = GetType().GetNestedType("Patches", BindingFlags.NonPublic | BindingFlags.Public);
            return patches == null ? Type.EmptyTypes : new[] { patches };
        }
    }

    public void TryLoad()
    {
        if (!Enabled)
        {
            Plugin.Log.LogInfo($"{Id}: disabled");
            return;
        }

        Harmony = new Harmony($"restless.core.{Id}");
        var failed = false;
        foreach (var type in PatchTypes)
            failed |= !ApplyEach(type);

        try
        {
            OnLoaded();
        }
        catch (Exception e)
        {
            failed = true;
            Plugin.Log.LogError($"{Id}: OnLoaded failed — {e}");
        }

        Plugin.Log.LogInfo(failed ? $"{Id}: loaded with patch errors" : $"{Id}: loaded");
    }

    public virtual void Tick()
    {
    }

    public virtual bool TickInMenus => false;

    protected virtual void OnLoaded()
    {
    }

    private bool ApplyEach(Type container)
    {
        var ok = true;
        foreach (var method in container.GetMethods(
                     BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            var attrs = method.GetCustomAttributes(false).OfType<HarmonyAttribute>().Select(a => a.info).ToList();
            if (attrs.Count == 0)
                continue;

            var prefix = method.GetCustomAttributes(typeof(HarmonyPrefix), false).Length > 0;
            var postfix = method.GetCustomAttributes(typeof(HarmonyPostfix), false).Length > 0;
            var transpiler = method.GetCustomAttributes(typeof(HarmonyTranspiler), false).Length > 0;
            var finalizer = method.GetCustomAttributes(typeof(HarmonyFinalizer), false).Length > 0;
            if (!prefix && !postfix && !transpiler && !finalizer)
                continue;

            try
            {
                var original = ResolveOriginal(attrs);
                if (original == null)
                {
                    ok = false;
                    Plugin.Log.LogError($"{Id}: skipped {method.Name} — target not found");
                    continue;
                }

                var patch = new HarmonyMethod(method);
                Harmony!.Patch(
                    original,
                    prefix: prefix ? patch : null,
                    postfix: postfix ? patch : null,
                    transpiler: transpiler ? patch : null,
                    finalizer: finalizer ? patch : null,
                    ilmanipulator: null);
            }
            catch (Exception e)
            {
                ok = false;
                Plugin.Log.LogError($"{Id}: skipped {method.Name} — {e.Message}");
            }
        }

        return ok;
    }

    private static MethodBase? ResolveOriginal(List<HarmonyMethod> attrs)
    {
        var merged = HarmonyMethod.Merge(attrs);
        var type = merged.declaringType;
        var name = merged.methodName;
        if (type == null || string.IsNullOrEmpty(name))
            return null;

        if (merged.argumentTypes != null)
            return AccessTools.DeclaredMethod(type, name, merged.argumentTypes)
                   ?? AccessTools.Method(type, name, merged.argumentTypes);

        try
        {
            return AccessTools.DeclaredMethod(type, name) ?? AccessTools.Method(type, name);
        }
        catch (AmbiguousMatchException)
        {
            return null;
        }
    }
}
