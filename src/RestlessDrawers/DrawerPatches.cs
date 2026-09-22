using HarmonyLib;

namespace RestlessDrawers;

[HarmonyPatch]
internal static class DrawerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Container), "Load")]
    private static void AfterLoad(Container __instance) => Face(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Container), "Save")]
    private static void AfterSave(Container __instance) => Face(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Container), "OnContainerChanged")]
    private static void AfterChanged(Container __instance) => Face(__instance);

    private static void Face(Container container)
    {
        if (container == null)
            return;
        var face = container.GetComponent<DrawerFace>();
        if (face != null)
            face.Refresh();
    }
}
