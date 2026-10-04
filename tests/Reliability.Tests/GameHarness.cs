public readonly record struct ZDOID(long Value)
{
    public static readonly ZDOID None = new(0);
}
public sealed class ZDO
{
    public ZDOID m_uid;
    public long Peer = 500;
    public long GetOwner() => Peer;
    public string GetString(string key) => _values.TryGetValue(key, out var value) && value is string text ? text : "";
    public void Set(string key, string value) => _values[key] = value;
    private readonly Dictionary<string, object> _values = new();
    public bool GetBool(string key) => _values.TryGetValue(key, out var value) && value is true;
    public ZDOID GetZDOID(string key) => _values.TryGetValue(key, out var value) && value is ZDOID id ? id : ZDOID.None;
    public void Set(string key, bool value) => _values[key] = value;
    public void Set(string key, ZDOID value) => _values[key] = value;
}
public sealed class ZNetView
{
    private readonly ZDO _zdo;
    public bool Owner;
    public int Claims;
    public readonly List<(string Rpc, ZPackage Data)> Sent = new();
    public void InvokeRPC(string rpc, ZPackage data) => Sent.Add((rpc, new ZPackage(data.GetArray())));
    public void InvokeRPC(long peer, string rpc, ZPackage data) => InvokeRPC(rpc, data);
    public ZNetView(long id, bool owner) { _zdo = new ZDO { m_uid = new(id) }; Owner = owner; }
    public bool IsValid() => true;
    public bool IsOwner() => Owner;
    public ZDO GetZDO() => _zdo;
    public void ClaimOwnership() { Owner = true; Claims++; }
}
public sealed class ZDOMan
{
    public static ZDOMan? instance;
    private readonly Dictionary<ZDOID, ZDO> _zdo = new();
    public void Add(ZDO zdo) => _zdo[zdo.m_uid] = zdo;
    public void Remove(ZDOID id) => _zdo.Remove(id);
    public ZDO? GetZDO(ZDOID id) => _zdo.TryGetValue(id, out var zdo) ? zdo : null;
}

public sealed class ZPackage
{
    private readonly MemoryStream _stream;
    private readonly BinaryReader _reader;
    private readonly BinaryWriter _writer;
    public ZPackage() : this(Array.Empty<byte>()) { }
    public ZPackage(byte[] bytes)
    { _stream = new MemoryStream(); _stream.Write(bytes); _stream.Position = 0; _reader = new(_stream); _writer = new(_stream); }
    public void SetPos(int pos) => _stream.Position = pos;
    public byte[] GetArray() => _stream.ToArray();
    public void Write(long value) => _writer.Write(value);
    public void Write(int value) => _writer.Write(value);
    public void Write(string value) => _writer.Write(value);
    public void Write(ZDOID value) => Write(value.Value);
    public long ReadLong() => _reader.ReadInt64();
    public int ReadInt() => _reader.ReadInt32();
    public string ReadString() => _reader.ReadString();
    public ZDOID ReadZDOID() => new(ReadLong());
}
public sealed class Player
{
    public static Player? m_localPlayer;
    public static readonly List<Player> All = new();
    public long Id;
    public ZNetView View = null!;
    public readonly Dictionary<string, string> m_customData = new();
    public long GetPlayerID() => Id;
    public static List<Player> GetAllPlayers() => All;
    public T? GetComponent<T>() where T : class => View as T;
}
public sealed class Container
{
    public ZNetView m_nview = null!;
    public bool Allowed = true;
    public int Stock;
    public bool CheckAccess(long actor) => Allowed;
    public bool IsOwner() => m_nview.IsOwner();
}
public sealed class ItemDrop
{
    public sealed class SharedData { public string m_name = ""; }
    public sealed class ItemData
    {
        public int m_stack, m_quality, m_variant, m_worldLevel;
        public float m_durability;
        public long m_crafterID;
        public SharedData m_shared = new();
        public GameObject? m_dropPrefab;
        public Dictionary<string,string> m_customData = new();
    }
}
public sealed class GameObject
{
    public string name = "";
    public Container Container = null!;
    public T? GetComponent<T>() where T : class => Container as T;
}
public sealed class ZNetScene
{
    public static ZNetScene? instance;
    public readonly Dictionary<ZDOID, GameObject> Objects = new();
    public GameObject? FindInstance(ZDOID id) => Objects.TryGetValue(id, out var go) ? go : null;
}
namespace UnityEngine { public static class Time { public static float unscaledTime; } }
namespace RestlessQoL.Core { internal sealed class HarnessMarker { } }
namespace RestlessQoL.Storage
{
    internal static class StorageSync { internal static bool WaitingFor(int id) => false; }
    internal static class NearbyStorage
    {
        internal static void PushOwned(Container target, ItemDrop.ItemData item, object? drop)
        { target.Stock += item.m_stack; item.m_stack = 0; }
    }
    internal static class StorageWithdraw
    {
        internal static bool WaitingFor(int id) => false;
        internal static ZPackage WriteItems(List<ItemDrop.ItemData> items)
        { var p = new ZPackage(); p.Write(items.Count); foreach (var item in items) p.Write(item.m_stack); return p; }
        internal static List<ItemDrop.ItemData> ReadItems(ZPackage pkg)
        { var result = new List<ItemDrop.ItemData>(); var count = pkg.ReadInt(); for (var i = 0; i < count; i++) result.Add(new() { m_stack = pkg.ReadInt() }); return result; }
        internal static void ReturnTo(Container target, List<ItemDrop.ItemData> items)
        { foreach (var item in items) target.Stock += item.m_stack; }
        internal static List<ItemDrop.ItemData> Within(List<ItemDrop.ItemData> held, List<ItemDrop.ItemData> overflow)
        { var count = Math.Min(held.Sum(i => i.m_stack), overflow.Sum(i => i.m_stack)); return new() { new() { m_stack = count } }; }
    }
}

public sealed class Smelter
{
    public ZNetView m_nview = new(60, true);
    private int _count;
    public void Queue(string name) { m_nview.GetZDO().Set("item" + _count, name); _count++; }
    public int GetQueueSize() => _count;
    public string GetQueuedOre() => m_nview.GetZDO().GetString("item0");
}

