using System.Globalization;

namespace RestlessQoL.Storage;

// Native QueueOre stores input at item + queue index. s_spawnOre is a separate
// completed-output buffer. The assembly contract is checked in NativeUi.Tests.
public static class ProductionQueue
{
    public static string Input(Smelter station, int index)
    {
        if (station?.m_nview == null || !station.m_nview.IsValid() || index < 0 || index >= station.GetQueueSize()) return "";
        return index == 0 ? station.GetQueuedOre()
            : station.m_nview.GetZDO().GetString("item" + index.ToString(CultureInfo.InvariantCulture));
    }
}
