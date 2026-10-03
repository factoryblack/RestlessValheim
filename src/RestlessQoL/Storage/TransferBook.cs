using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace RestlessQoL.Storage;

// Pure transaction history. Terminal records cannot become reservations again.
// Retired sequence floors reject old packets after compacting the history.
internal sealed class TransferBook
{
    internal enum Phase { Held, Committed, Cancelled }
    internal sealed class Entry
    {
        internal long Actor;
        internal int Id, Count;
        internal string Kind = "", Payload = "";
        internal Phase State;
    }
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _floors = new(StringComparer.Ordinal);
    private static string ActorKey(long actor, string kind) => actor.ToString(CultureInfo.InvariantCulture) + ":" + kind;
    private static string Key(long actor, int id, string kind) => ActorKey(actor, kind) + ":" + id;
    internal Entry? Find(long actor, int id, string kind) => _entries.TryGetValue(Key(actor, id, kind), out var row) ? row : null;
    internal bool Retired(long actor, int id, string kind) => id <= 0 || (_floors.TryGetValue(ActorKey(actor, kind), out var floor) && id <= floor);
    internal Entry Prepare(long actor, int id, string kind, Func<Entry> reserve)
    {
        var row = Find(actor, id, kind);
        if (row != null) return row;
        row = Retired(actor, id, kind) ? new Entry { State = Phase.Cancelled } : reserve();
        row.Actor = actor; row.Id = id; row.Kind = kind;
        _entries[Key(actor, id, kind)] = row;
        return row;
    }
    internal bool Resolve(long actor, int id, string kind, bool commit, Action<Entry> apply)
    {
        var row = Find(actor, id, kind);
        if (row == null)
        {
            // A cancellation arriving before its request must still close it.
            if (commit) return Retired(actor, id, kind);
            row = Prepare(actor, id, kind, () => new Entry { State = Phase.Cancelled });
        }
        if (row.State != Phase.Held) return true;
        apply(row);
        row.State = commit ? Phase.Committed : Phase.Cancelled;
        row.Payload = "";
        return true;
    }
    internal string Save()
    {
        // Keep unresolved payloads; only compact settled history.
        foreach (var group in _entries.Values.Where(e => e.State != Phase.Held).GroupBy(e => ActorKey(e.Actor, e.Kind)).ToArray())
        {
            var old = group.OrderByDescending(e => e.Id).Skip(128).ToArray();
            foreach (var row in old)
            {
                _floors.TryGetValue(group.Key, out var floor);
                _floors[group.Key] = Math.Max(floor, row.Id);
                _entries.Remove(Key(row.Actor, row.Id, row.Kind));
            }
        }
        var text = new StringBuilder();
        foreach (var floor in _floors.OrderBy(p => p.Key)) text.Append("F\t").Append(floor.Key).Append('\t').Append(floor.Value).Append('\n');
        foreach (var row in _entries.Values.OrderBy(e => e.Actor).ThenBy(e => e.Id).ThenBy(e => e.Kind))
            text.Append("R\t").Append(row.Actor).Append('\t').Append(row.Id).Append('\t').Append(row.Kind).Append('\t')
                .Append((int)row.State).Append('\t').Append(row.Count).Append('\t').Append(row.Payload).Append('\n');
        return text.ToString();
    }
    internal static TransferBook Load(string text)
    {
        var book = new TransferBook();
        foreach (var line in text.Split('\n'))
        {
            if (line.Length == 0) continue;
            var p = line.Split('\t');
            if (p.Length == 3 && p[0] == "F") { book._floors[p[1]] = int.Parse(p[2], CultureInfo.InvariantCulture); continue; }
            if (p.Length != 7 || p[0] != "R") throw new FormatException("Invalid transfer history");
            var row = new Entry { Actor = long.Parse(p[1], CultureInfo.InvariantCulture), Id = int.Parse(p[2], CultureInfo.InvariantCulture),
                Kind = p[3], State = (Phase)int.Parse(p[4], CultureInfo.InvariantCulture), Count = int.Parse(p[5], CultureInfo.InvariantCulture), Payload = p[6] };
            if ((int)row.State < (int)Phase.Held || (int)row.State > (int)Phase.Cancelled || row.Count < 0) throw new FormatException("Invalid transfer state");
            book._entries.Add(Key(row.Actor, row.Id, row.Kind), row);
        }
        return book;
    }
}
