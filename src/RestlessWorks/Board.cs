using UnityEngine;

namespace RestlessWorks;

public class Board : MonoBehaviour, Hoverable, Interactable
{
    public string GetHoverName() => Localization.instance.Localize(BoardPiece.DisplayName);

    public string GetHoverText() => Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] Work orders");

    public float GetHoverOffset() => 0.4f;

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || alt || user is not Player player || player != Player.m_localPlayer)
            return false;
        var view = GetComponent<ZNetView>();
        if (view != null && view.IsValid() && !view.IsOwner())
            view.ClaimOwnership();
        var piece = GetComponent<Piece>();
        if (piece != null)
            Works.Raise(piece);
        return true;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
}
