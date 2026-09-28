using HarmonyLib;
using RestlessQoL.Api;
using UnityEngine;

namespace RestlessStorage;

public class Storekeeper : MonoBehaviour, Hoverable, Interactable
{
    private static StorageSource? _openSource;
    private StorageSource? _source;
    public string GetHoverName() => Localization.instance.Localize(StorekeeperPiece.DisplayName);
    public string GetHoverText() => Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] Storage network");
    public float GetHoverOffset() => 0.4f;
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || alt || user is not Player player || player != Player.m_localPlayer) return false;
        if (!Plugin.WindowEnabled.Value)
        {
            player.Message(MessageHud.MessageType.Center, "Enable the storage browser in F8 · RestlessStorage.");
            return true;
        }
        _source = new StorageSource(this, player);
        if (StorageWindowApi.Open(_source)) _openSource = _source;
        return true;
    }
    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    private void OnDisable() { if (_source != null) StorageWindowApi.Close(_source); }
    internal static void CloseWindow()
    {
        if (_openSource != null) StorageWindowApi.Close(_openSource);
        _openSource = null;
    }
    [HarmonyPatch(typeof(Menu), "Update")]
    private static class MenuInput
    {
        [HarmonyPrefix]
        private static bool Prefix() => !StorageWindowApi.BlocksMenuInput;
    }
}
