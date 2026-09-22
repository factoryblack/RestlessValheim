using System.Collections.Generic;
using UnityEngine;

namespace RestlessDrawers;

// Visual only. The cabinet is one Container; fronts never open their own inventory.
public sealed class DrawerFace : MonoBehaviour
{
    private const int Slots = 4;
    private const float IconSize = 0.14f;
    private readonly SpriteRenderer[] _faces = new SpriteRenderer[Slots];
    private string _stamp = "";

    private void Awake() => Ensure();

    private void OnEnable() => Refresh();

    public void Refresh()
    {
        Ensure();
        var box = GetComponent<Container>();
        var inv = box != null ? box.GetInventory() : null;
        var top = Top(inv);
        var stamp = Signature(top);
        if (stamp == _stamp)
            return;
        _stamp = stamp;
        for (var i = 0; i < Slots; i++)
        {
            var face = _faces[i];
            if (face == null)
                continue;
            if (i >= top.Count)
            {
                face.enabled = false;
                face.sprite = null;
                continue;
            }

            face.sprite = top[i];
            face.enabled = top[i] != null;
        }
    }

    private void Ensure()
    {
        var visual = transform.Find(DrawerVisual.ChildName);
        if (visual == null)
            return;
        var filter = visual.GetComponent<MeshFilter>();
        var bounds = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one * 0.5f);
        var z = bounds.center.z + bounds.extents.z + 0.012f;
        var y = bounds.center.y;
        var span = Mathf.Min(bounds.size.x * 0.72f, IconSize * Slots);
        var left = bounds.center.x - span * 0.5f + span / Slots * 0.5f;
        for (var i = 0; i < Slots; i++)
        {
            if (_faces[i] != null)
                continue;
            var go = new GameObject("RestlessDrawerFace" + i);
            go.layer = gameObject.layer;
            go.transform.SetParent(visual, false);
            go.transform.localPosition = new Vector3(left + i * (span / Slots), y, z);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var face = go.AddComponent<SpriteRenderer>();
            face.drawMode = SpriteDrawMode.Simple;
            face.color = Color.white;
            face.enabled = false;
            var scale = IconSize / Mathf.Max(0.01f, visual.lossyScale.x);
            go.transform.localScale = new Vector3(scale, scale, scale);
            _faces[i] = face;
        }
    }

    private static List<Sprite> Top(Inventory? inv)
    {
        var list = new List<Sprite>();
        if (inv == null)
            return list;
        var counts = new Dictionary<string, (int count, Sprite? icon)>();
        foreach (var item in inv.GetAllItems())
        {
            if (item?.m_shared == null)
                continue;
            var key = item.m_shared.m_name;
            counts.TryGetValue(key, out var cur);
            counts[key] = (cur.count + Mathf.Max(1, item.m_stack), item.GetIcon());
        }

        var ranked = new List<(int count, Sprite? icon)>(counts.Values);
        ranked.Sort((a, b) => b.count.CompareTo(a.count));
        foreach (var row in ranked)
        {
            if (row.icon == null)
                continue;
            list.Add(row.icon);
            if (list.Count >= Slots)
                break;
        }

        return list;
    }

    private static string Signature(List<Sprite> icons)
    {
        var s = icons.Count.ToString();
        foreach (var icon in icons)
            s += ":" + (icon != null ? icon.name : "");
        return s;
    }
}
