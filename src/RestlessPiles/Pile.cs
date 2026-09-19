using System;
using System.Collections;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessPiles;

internal static class Pile
{
    private const string CountKey = "restless.stored";
    private const string LongKey = "restless.pile";

    public static bool IsPilePrefab(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        return name.EndsWith("_stack", StringComparison.Ordinal)
               || name.EndsWith("_pile", StringComparison.Ordinal);
    }

    public static bool TryKind(GameObject go, out Piece piece, out ItemDrop.ItemData item)
    {
        piece = go != null ? go.GetComponent<Piece>() : null;
        item = null;
        if (piece?.m_resources == null || piece.m_resources.Length == 0)
            return false;
        if (!IsPilePrefab(Utils.GetPrefabName(go)))
            return false;
        var drop = piece.m_resources[0].m_resItem;
        item = drop?.m_itemData;
        return item?.m_shared != null && item.m_shared.m_maxStackSize > 1;
    }

    public static PileBox? TryAttach(GameObject go)
    {
        if (!TryKind(go, out _, out _))
            return go != null ? go.GetComponent<PileBox>() : null;
        var box = go.GetComponent<PileBox>();
        return box != null ? box : go.AddComponent<PileBox>();
    }

    public static int Count(ZNetView view)
    {
        var zdo = view != null && view.IsValid() ? view.GetZDO() : null;
        if (zdo == null)
            return 0;
        var stored = zdo.GetLong(LongKey, -1L);
        if (stored >= 0)
            return stored > int.MaxValue ? int.MaxValue : (int)stored;
        return zdo.GetInt(CountKey, 0);
    }

    public static void Write(ZNetView view, int stack)
    {
        if (view == null || !view.IsValid() || !view.IsOwner())
            return;
        var zdo = view.GetZDO();
        if (zdo == null)
            return;
        var n = Mathf.Max(0, stack);
        zdo.Set(LongKey, (long)n);
        zdo.Set(CountKey, n);
    }

    public static void OwnThen(ZNetView view, Action<ZNetView> act, Action? fail = null)
    {
        if (view == null || !view.IsValid())
        {
            fail?.Invoke();
            return;
        }

        if (view.IsOwner())
        {
            act(view);
            return;
        }

        view.ClaimOwnership();
        Plugin.Instance.StartCoroutine(WaitOwn(view, act, fail));
    }

    private static IEnumerator WaitOwn(ZNetView view, Action<ZNetView> act, Action? fail)
    {
        var until = Time.time + 1.5f;
        while (view != null && view.IsValid() && !view.IsOwner() && Time.time < until)
            yield return null;
        if (view != null && view.IsValid() && view.IsOwner())
            act(view);
        else
            fail?.Invoke();
    }

    public static string Title(ItemDrop.ItemData item)
    {
        var name = item?.m_shared?.m_name ?? "";
        if (Localization.instance != null)
            name = Localization.instance.Localize(name);
        return RestlessUi.Bare(name);
    }

    public static string PieceTitle(Piece piece)
    {
        var name = piece?.m_name ?? "";
        if (Localization.instance != null)
            name = Localization.instance.Localize(name);
        return RestlessUi.Bare(name);
    }
}
