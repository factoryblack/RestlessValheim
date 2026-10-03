public readonly record struct ZDOID(long Value)
{
    public static readonly ZDOID None = new(0);
}
public sealed class ZDO
{
    public ZDOID m_uid;
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
