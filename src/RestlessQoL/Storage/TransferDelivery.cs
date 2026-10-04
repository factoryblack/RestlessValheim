using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using RestlessQoL.Core;

namespace RestlessQoL.Storage;

// Chest reservations live on its ZDO, while the character remembers its decision.
// A missed acknowledgement never causes an automatic refund of delivered items.
internal static class TransferDelivery
{
    internal const string DecisionRpc = "RestlessTransferDecisionV2", ClosedRpc = "RestlessTransferClosedV2";
    private const string Journal = "restless.transfer.v2", Receipts = "restless.transfer.receipts.v2", Sequence = "restless.transfer.sequence.v2";
    private static float _nextRetry;
    private static int _sessionSequence;
    internal static int NextId()
    {
        var player = Player.m_localPlayer;
        if (player == null) return 0;
        player.m_customData.TryGetValue(Sequence, out var text);
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var last);
        last = Math.Max(last, _sessionSequence);
        if (last == int.MaxValue) throw new InvalidOperationException("Transfer sequence exhausted");
        var id = Math.Max(0, last) + 1;
        _sessionSequence = id;
        player.m_customData[Sequence] = id.ToString(CultureInfo.InvariantCulture);
        return id;
    }
    internal static bool ActorIsPeer(long actor, long sender)
    {
        foreach (var player in Player.GetAllPlayers())
        {
            if (player == null || player.GetPlayerID() != actor) continue;
            var view = player.GetComponent<ZNetView>();
            if (view != null && view.IsValid() && view.GetZDO().GetOwner() == sender) return true;
        }
        return false;
    }
    internal static bool MayUse(Container container, long actor, long sender) =>
        actor != 0 && ActorIsPeer(actor, sender) && container.CheckAccess(actor);
    internal static TransferBook Read(Container container) => TransferBook.Load(container.m_nview.GetZDO().GetString(Journal));
    internal static void Write(Container container, TransferBook book) => container.m_nview.GetZDO().Set(Journal, book.Save());
    internal static string Pack(List<ItemDrop.ItemData> items) => Convert.ToBase64String(StorageWithdraw.WriteItems(items).GetArray());
    internal static List<ItemDrop.ItemData> Unpack(string data) => string.IsNullOrEmpty(data) ? new List<ItemDrop.ItemData>() : StorageWithdraw.ReadItems(new ZPackage(Convert.FromBase64String(data)));
    internal static string ReceiptKey(int id, string kind) => id.ToString(CultureInfo.InvariantCulture) + ":" + kind;
    internal static void Remember(Container container, int id, string kind, int decision, string overflow = "")
    {
        var player = Player.m_localPlayer;
        if (player == null || id <= 0) return;
        var receipts = ReadReceipts(player);
        var pkg = new ZPackage();
        pkg.Write(player.GetPlayerID()); pkg.Write(id); pkg.Write(kind); pkg.Write(container.m_nview.GetZDO().m_uid); pkg.Write(decision); pkg.Write(overflow);
        receipts[ReceiptKey(id, kind)] = Convert.ToBase64String(pkg.GetArray());
        SaveReceipts(player, receipts);
        if (decision != 0) Send(container, pkg);
    }
    internal static void Tick()
    {
        if (Time.unscaledTime < _nextRetry) return;
        _nextRetry = Time.unscaledTime + 2f;
        var player = Player.m_localPlayer;
        if (player == null || ZNetScene.instance == null) return;
        var receipts = ReadReceipts(player);
        var changed = false;
        foreach (var pair in new List<KeyValuePair<string, string>>(receipts))
        {
            var pkg = new ZPackage(Convert.FromBase64String(pair.Value));
            var actor = pkg.ReadLong(); var id = pkg.ReadInt(); var kind = pkg.ReadString(); var target = pkg.ReadZDOID(); var decision = pkg.ReadInt(); var overflow = pkg.ReadString();
            if (actor != player.GetPlayerID()) continue;
            if (decision == 0 && !StorageSync.WaitingFor(id) && !StorageWithdraw.WaitingFor(id))
            {
                // After reconnect there is no callback to deliver this request.
                decision = -1;
                pkg = new ZPackage(); pkg.Write(actor); pkg.Write(id); pkg.Write(kind); pkg.Write(target); pkg.Write(decision); pkg.Write(overflow);
                receipts[pair.Key] = Convert.ToBase64String(pkg.GetArray()); changed = true;
            }
            if (decision == 0) continue;
            var go = ZNetScene.instance.FindInstance(target);
            var container = go != null ? go.GetComponent<Container>() : null;
            if (container != null && container.m_nview != null && container.m_nview.IsValid()) Send(container, pkg);
        }
        if (changed) SaveReceipts(player, receipts);
    }
    private static void Send(Container container, ZPackage pkg)
    {
        if (container?.m_nview == null || !container.m_nview.IsValid()) return;
        pkg.SetPos(0); container.m_nview.InvokeRPC(DecisionRpc, pkg);
    }
    internal static void OnDecision(Container container, long sender, ZPackage pkg)
    {
        if (!container.IsOwner()) return;
        pkg.SetPos(0);
        var actor = pkg.ReadLong(); var id = pkg.ReadInt(); var kind = pkg.ReadString(); var target = pkg.ReadZDOID(); var decision = pkg.ReadInt(); var overflow = pkg.ReadString();
        if (id <= 0 || target != container.m_nview.GetZDO().m_uid || !ActorIsPeer(actor, sender) || (kind != "P" && kind != "S" && kind != "W") || (decision != 1 && decision != -1)) return;
        // Access may change after reservation; settling it must still be possible.
        var book = Read(container);
        if (!book.Resolve(actor, id, kind, decision == 1, row =>
        {
            var items = Unpack(row.Payload);
            if (decision == -1)
            {
                if (kind != "S") StorageWithdraw.ReturnTo(container, items);
            }
            else if (kind == "S")
            {
                foreach (var item in items)
                {
                    NearbyStorage.PushOwned(container, item, null);
                    if (item.m_stack > 0) StorageWithdraw.ReturnTo(container, new List<ItemDrop.ItemData> { item });
                }
            }
            else if (kind == "W") StorageWithdraw.ReturnTo(container, StorageWithdraw.Within(items, Unpack(overflow)));
        })) return;
        Write(container, book);
        var reply = new ZPackage(); reply.Write(actor); reply.Write(id); reply.Write(kind);
        container.m_nview.InvokeRPC(sender, ClosedRpc, reply);
    }
    internal static void OnClosed(Container container, long sender, ZPackage pkg)
    {
        var player = Player.m_localPlayer;
        if (player == null || container?.m_nview == null || !container.m_nview.IsValid() || container.m_nview.GetZDO().GetOwner() != sender) return;
        pkg.SetPos(0); var actor = pkg.ReadLong(); var id = pkg.ReadInt(); var kind = pkg.ReadString();
        if (actor != player.GetPlayerID()) return;
        var receipts = ReadReceipts(player);
        var key = ReceiptKey(id, kind);
        if (!receipts.TryGetValue(key, out var data)) return;
        var saved = new ZPackage(Convert.FromBase64String(data)); saved.ReadLong(); saved.ReadInt(); saved.ReadString();
        if (saved.ReadZDOID() != container.m_nview.GetZDO().m_uid) return;
        receipts.Remove(key); SaveReceipts(player, receipts);
    }
    private static Dictionary<string, string> ReadReceipts(Player player)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!player.m_customData.TryGetValue(Receipts, out var text)) return map;
        foreach (var line in text.Split('\n'))
        {
            var split = line.IndexOf('\t');
            if (split > 0) map[line.Substring(0, split)] = line.Substring(split + 1);
        }
        return map;
    }
    private static void SaveReceipts(Player player, Dictionary<string, string> receipts)
    {
        if (receipts.Count == 0) { player.m_customData.Remove(Receipts); return; }
        var lines = new List<string>();
        foreach (var pair in receipts) lines.Add(pair.Key + "\t" + pair.Value);
        player.m_customData[Receipts] = string.Join("\n", lines);
    }
}
