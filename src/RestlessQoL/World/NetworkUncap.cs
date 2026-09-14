using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using HarmonyLib;
using RestlessQoL.Core;
using Steamworks;

namespace RestlessQoL.World;

// Vanilla SendZDOs stops once the peer queue is 10 KB. Steam then caps the
// pipe at 150 KB/s. Distant players/mobs freeze, then snap. Raise the window
// and SendRateMax only — leave SendRateMin so Steam can still back off.
public sealed class NetworkUncap : FeatureModule
{
    public override string Id => "net.uncap";
    public override bool Enabled => true;

    private const int VanillaWindow = 10240;
    private const int VanillaRate = 153600;

    protected override void OnLoaded()
    {
        ModConfig.NetworkUncapEnabled.SettingChanged += (_, _) => ApplySteam();
    }

    private static int Window() =>
        ModConfig.NetworkUncapEnabled.Value ? 32768 : VanillaWindow;

    private static int Rate() =>
        ModConfig.NetworkUncapEnabled.Value ? 524288 : VanillaRate;

    private static void ApplySteam()
    {
        var boxed = (object)Rate();
        var handle = GCHandle.Alloc(boxed, GCHandleType.Pinned);
        try
        {
            SteamNetworkingUtils.SetConfigValue(
                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMax,
                ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global,
                IntPtr.Zero,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
                handle.AddrOfPinnedObject());
        }
        catch (Exception e)
        {
            if (e.Message.IndexOf("not initialized", StringComparison.OrdinalIgnoreCase) < 0)
                Plugin.Log.LogWarning("net.uncap steam: " + e.Message);
        }
        finally
        {
            handle.Free();
        }
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.SendZDOs))]
        private static IEnumerable<CodeInstruction> SendWindow(IEnumerable<CodeInstruction> instructions)
        {
            var call = AccessTools.Method(typeof(NetworkUncap), nameof(Window));
            var replaced = 0;
            foreach (var ins in instructions)
            {
                if (ins.opcode == OpCodes.Ldc_I4 && ins.operand is int n && n == VanillaWindow)
                {
                    replaced++;
                    var next = new CodeInstruction(OpCodes.Call, call);
                    next.labels.AddRange(ins.labels);
                    next.blocks.AddRange(ins.blocks);
                    yield return next;
                }
                else
                    yield return ins;
            }

            if (replaced == 0)
                Plugin.Log.LogWarning("net.uncap: SendZDOs has no 10240 window — 1.0 moved the cap, this feature is a no-op there.");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ZSteamSocket), nameof(ZSteamSocket.RegisterGlobalCallbacks))]
        private static void AfterSteam() => ApplySteam();
    }
}
