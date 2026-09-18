using System.Collections.Generic;
using RestlessQoL.Storage;
using UnityEngine;

namespace RestlessPiles;

public sealed class PileBox : MonoBehaviour, Interactable, Hoverable
{
    internal static readonly List<PileBox> All = new();

    private ZNetView _view = null!;
    private Piece _piece = null!;
    private ItemDrop.ItemData _item = null!;

    public ZNetView View => _view;
    public Piece Piece => _piece;
    public ItemDrop.ItemData Item => _item;
    public int Stored => Pile.Count(_view);

    private void Awake()
    {
        _view = GetComponent<ZNetView>();
        if (!Pile.TryKind(gameObject, out _piece, out _item))
        {
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (!All.Contains(this))
            All.Add(this);
    }

    private void OnDisable() => All.Remove(this);

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (!PileConfig.On || hold || user != Player.m_localPlayer)
            return false;
        PileUi.Open(this);
        return true;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        if (!PileConfig.On || user != Player.m_localPlayer || !PileBag.Matches(item, _item))
            return false;
        return PileBag.DumpInto(Player.m_localPlayer, this) > 0;
    }

    public string GetHoverText()
    {
        if (!PileConfig.On)
            return GetHoverName();
        return Localization.instance.Localize(
            GetHoverName() + " x" + Stored + "\n[<color=yellow><b>$KEY_Use</b></color>] Open");
    }

    public string GetHoverName()
    {
        var title = Pile.PieceTitle(_piece);
        return title.Length > 0 ? title : Pile.Title(_item);
    }

    public float GetHoverOffset() => 0f;

    internal void Spill()
    {
        if (_view == null || !_view.IsValid() || !_view.IsOwner())
            return;
        var left = Stored;
        if (left <= 0)
            return;
        Pile.Write(_view, 0);
        NearbyStorage.EnsureDropPrefab(_item);
        if (_item.m_dropPrefab == null)
            return;
        var max = Mathf.Max(1, _item.m_shared.m_maxStackSize);
        var pos = transform.position + Vector3.up * 0.5f;
        while (left > 0)
        {
            var n = Mathf.Min(max, left);
            ItemDrop.DropItem(_item, n, pos, Quaternion.identity);
            left -= n;
        }
    }
}
