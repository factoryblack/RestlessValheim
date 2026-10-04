using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RestlessQoL.Storage;

// Same string on every peer. Stack size is not part of it, so a take can
// split a stack without becoming a different item.
public static class ItemKey
{
    public static string Of(ItemDrop.ItemData item)
    {
        if (item?.m_shared == null)
            return "";
        var text = new StringBuilder();
        Part(text, item.m_dropPrefab != null ? item.m_dropPrefab.name : item.m_shared.m_name);
        Part(text, item.m_quality.ToString(CultureInfo.InvariantCulture));
        Part(text, item.m_variant.ToString(CultureInfo.InvariantCulture));
        Part(text, item.m_worldLevel.ToString(CultureInfo.InvariantCulture));
        Part(text, item.m_durability.ToString("R", CultureInfo.InvariantCulture));
        Part(text, item.m_crafterID.ToString(CultureInfo.InvariantCulture));
        if (item.m_customData != null)
        {
            var keys = new List<string>(item.m_customData.Keys);
            keys.Sort(StringComparer.Ordinal);
            foreach (var key in keys)
            {
                Part(text, key);
                item.m_customData.TryGetValue(key, out var value);
                Part(text, value ?? "");
            }
        }

        return text.ToString();
    }

    private static void Part(StringBuilder text, string value)
    {
        value ??= "";
        text.Append(value.Length).Append(':').Append(value);
    }
}

