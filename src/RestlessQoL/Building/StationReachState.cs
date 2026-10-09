namespace RestlessQoL.Building;

internal sealed class StationReachState
{
    private readonly float _use, _build, _extra;
    public StationReachState(float use, float build, float extra) { _use = use; _build = build; _extra = extra; }
    public void Resolve(bool enabled, float reach, out float use, out float build, out float extra)
    {
        use = enabled ? reach : _use;
        build = enabled ? reach : _build;
        extra = enabled ? 0f : _extra;
    }
}
