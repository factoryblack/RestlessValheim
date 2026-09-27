using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Jotunn.Managers;
using UnityEngine;

namespace RestlessQoL.Core;

// Wraps vanilla PlayerStats. Other modules read kills from here — do not
// start a second counter. Extras are only keys vanilla does not have.
public sealed class Ledger : FeatureModule
{
    public override string Id => "player.ledger";
    public override bool Enabled => true;
    public override bool TickInMenus => true;

    public const string ExtraKey = "restless.ledger.extra";

    private static Player? _bound;
    private static readonly Dictionary<string, float> Extra = new(StringComparer.OrdinalIgnoreCase);

    protected override void OnLoaded()
    {
        GUIManager.OnCustomGUIAvailable += () =>
        {
            _bound = null;
            Extra.Clear();
        };
    }

    public override void Tick() => Bind(Player.m_localPlayer);

    // Valheim's GetStat follows the achievement record once achievements are
    // allowed. That record stayed empty while the game was marked modded.
    // The character page wants the lifetime record, which is always index 0.
    public static float Get(PlayerStatType stat)
    {
        var bag = Lifetime();
        return bag != null ? bag[stat] : 0f;
    }

    public static float Enemy(string prefab)
    {
        if (string.IsNullOrEmpty(prefab))
            return 0f;
        var total = Kills();
        return total != null && total.TryGetValue(prefab, out var n) ? n : 0f;
    }

    public static IEnumerable<KeyValuePair<string, float>> Hunts()
    {
        var totals = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        var total = Kills();
        if (total != null)
        {
            foreach (var pair in total)
            {
                if (string.IsNullOrEmpty(pair.Key) || pair.Value <= 0f)
                    continue;
                totals.TryGetValue(pair.Key, out var cur);
                totals[pair.Key] = cur + pair.Value;
            }
        }

        var list = new List<KeyValuePair<string, float>>(totals);
        list.Sort((a, b) => b.Value.CompareTo(a.Value));
        return list;
    }

    public static void Bump(string key, float delta)
    {
        if (string.IsNullOrEmpty(key) || Mathf.Approximately(delta, 0f) || Player.m_localPlayer == null)
            return;
        Bind(Player.m_localPlayer);
        Extra.TryGetValue(key, out var cur);
        Extra[key] = cur + delta;
        Save(Player.m_localPlayer);
    }

    public static float GetExtra(string key)
    {
        Bind(Player.m_localPlayer);
        Extra.TryGetValue(key, out var value);
        return value;
    }

    public static IEnumerable<KeyValuePair<string, float>> Extras()
    {
        Bind(Player.m_localPlayer);
        var list = new List<KeyValuePair<string, float>>(Extra);
        list.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));
        return list;
    }

    public static string EnemyName(string prefab)
    {
        if (string.IsNullOrEmpty(prefab))
            return "";
        var key = prefab.Replace("(Clone)", "").Trim();
        if (key.IndexOf('$') >= 0)
        {
            var fromToken = RestlessUi.Bare(key);
            if (Good(fromToken))
                return fromToken;
        }

        var go = ZNetScene.instance?.GetPrefab(key);
        var character = go != null ? go.GetComponent<Character>() : null;
        if (character != null)
        {
            var named = RestlessUi.Bare(character.m_name);
            if (Good(named))
                return named;
            var hover = RestlessUi.Bare(character.GetHoverName());
            if (Good(hover))
                return hover;
        }

        var slim = key;
        if (slim.StartsWith("enemy_", System.StringComparison.OrdinalIgnoreCase))
            slim = slim.Substring(6);
        foreach (var token in new[]
                 {
                     "$enemy_" + slim,
                     "$enemy_" + slim.ToLowerInvariant(),
                     "$enemy_" + slim.Replace("_", "").ToLowerInvariant(),
                     "$" + key.TrimStart('$')
                 })
        {
            var loc = RestlessUi.Bare(token);
            if (Good(loc) && !loc.Equals(token, StringComparison.OrdinalIgnoreCase))
                return loc;
        }

        return Humanize(key);
    }

    private static bool Good(string name) =>
        name.Length > 0 && name.IndexOf('$') < 0;

    private static string Humanize(string raw)
    {
        var s = raw.Trim().TrimStart('$');
        if (s.StartsWith("enemy_", StringComparison.OrdinalIgnoreCase))
            s = s.Substring(6);
        s = s.Replace('_', ' ').Replace('.', ' ');
        var sb = new StringBuilder(s.Length + 4);
        for (var i = 0; i < s.Length; i++)
        {
            if (i > 0 && char.IsUpper(s[i]) && !char.IsUpper(s[i - 1]) && s[i - 1] != ' ')
                sb.Append(' ');
            sb.Append(i == 0 ? char.ToUpperInvariant(s[i]) : s[i]);
        }

        return sb.ToString();
    }

    private static PlayerProfile.PlayerStats? Lifetime()
    {
        var all = Game.instance?.GetPlayerProfile()?.m_playerStats;
        return all != null && all.Length > 0 ? all[0] : null;
    }

    private static Dictionary<string, float>? Kills()
    {
        var stats = Lifetime()?.m_enemyStats;
        return stats != null && stats.Length > 0 ? stats[0] : null;
    }

    private static void Bind(Player? player)
    {
        if (player == _bound)
            return;
        _bound = player;
        Extra.Clear();
        if (player?.m_customData == null || !player.m_customData.TryGetValue(ExtraKey, out var raw)
            || string.IsNullOrEmpty(raw))
            return;
        foreach (var part in raw.Split('|'))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0)
                continue;
            var key = part.Substring(0, eq).Trim();
            if (key.Length == 0)
                continue;
            if (!float.TryParse(part.Substring(eq + 1), NumberStyles.Float, CultureInfo.InvariantCulture,
                    out var value))
                continue;
            Extra[key] = value;
        }
    }

    private static void Save(Player? player)
    {
        if (player?.m_customData == null)
            return;
        if (Extra.Count == 0)
        {
            player.m_customData.Remove(ExtraKey);
            return;
        }

        var sb = new StringBuilder();
        foreach (var pair in Extra)
        {
            if (sb.Length > 0)
                sb.Append('|');
            sb.Append(pair.Key);
            sb.Append('=');
            sb.Append(pair.Value.ToString("G", CultureInfo.InvariantCulture));
        }

        player.m_customData[ExtraKey] = sb.ToString();
    }
}
