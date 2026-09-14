using System.Linq;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.Storage;

public sealed class GroundVacuum : FeatureModule
{
    public override string Id => "storage.vacuum";
    public override bool Enabled => ModConfig.StorageEnabled.Value && ModConfig.VacuumEnabled.Value;
    public override bool TickInMenus => true;

    private float _next;

    public override void Tick()
    {
        if (!Enabled)
            return;
        if (Time.time < _next)
            return;
        _next = Time.time + ModConfig.VacuumInterval.Value;

        var player = Player.m_localPlayer;
        if (player == null)
            return;

        var range = ModConfig.StorageRange.Value;
        var origin = player.transform.position;
        foreach (var drop in ItemDrop.s_instances.ToArray())
        {
            if (drop == null || drop.m_itemData?.m_shared == null)
                continue;
            if (drop.m_itemData.m_shared.m_questItem)
                continue;
            if ((drop.transform.position - origin).sqrMagnitude > range * range)
                continue;
            NearbyStorage.TryDepositDrop(player, drop);
        }
    }
}
