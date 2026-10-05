namespace RestlessQoL.Storage;

// An active controller keeps its native queues across peer ownership changes.
// Browsing never calls Acquire. Two controllers cannot knowingly take each other's work.
public static class ProductionLease
{
    private const string OwnerKey = "restless.production.controller.v1";
    private const string ActiveKey = "restless.production.active.v1";
    public static void SetActive(ZNetView controller, bool active)
    {
        if (controller != null && controller.IsValid() && controller.IsOwner()
            && controller.GetZDO().GetBool(ActiveKey) != active) controller.GetZDO().Set(ActiveKey, active);
    }
    public static bool Available(ZNetView machine, ZNetView controller)
    {
        if (machine == null || controller == null || !machine.IsValid() || !controller.IsValid()) return false;
        var assigned = machine.GetZDO().GetZDOID(OwnerKey);
        if (assigned == ZDOID.None || assigned == controller.GetZDO().m_uid) return true;
        var previous = ZDOMan.instance != null ? ZDOMan.instance.GetZDO(assigned) : null;
        return previous == null || !previous.GetBool(ActiveKey);
    }
    public static bool Controls(ZNetView machine, ZNetView controller)
    {
        return machine != null && controller != null && machine.IsValid() && controller.IsValid()
            && controller.GetZDO().GetBool(ActiveKey)
            && machine.GetZDO().GetZDOID(OwnerKey) == controller.GetZDO().m_uid;
    }
    public static bool Acquire(ZNetView machine, ZNetView controller)
    {
        if (controller == null || !controller.IsValid() || !controller.IsOwner() || !Available(machine, controller)) return false;
        if (!machine.IsOwner()) machine.ClaimOwnership();
        if (!machine.IsOwner() || !Available(machine, controller)) return false;
        var owner = controller.GetZDO().m_uid;
        if (machine.GetZDO().GetZDOID(OwnerKey) != owner) machine.GetZDO().Set(OwnerKey, owner);
        return true;
    }
}
