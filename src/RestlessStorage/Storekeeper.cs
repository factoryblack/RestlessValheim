using UnityEngine;

namespace RestlessStorage;

// The placed table. Use names it; the storage window is not on this piece yet.
public class Storekeeper : MonoBehaviour, Hoverable, Interactable
{
    public string GetHoverName() => Localization.instance.Localize(StorekeeperPiece.DisplayName);

    public string GetHoverText() =>
        Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] Storage network");

    public float GetHoverOffset() => 0.4f;

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || alt || user is not Player player)
            return false;
        player.Message(MessageHud.MessageType.Center, "The storage network is not open yet.");
        return true;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
}
