using System;
using System.Globalization;

namespace RestlessCook;

internal static partial class KitchenRun
{
    private sealed class TapReceipt
    {
        internal string StationId = "", Output = "";
        internal int Amount;
        internal bool Committed;
        internal long Due;
    }
    private static string TapLine(TapReceipt tap) => "T\t" + tap.StationId + "\t" + tap.Output + "\t"
        + tap.Amount.ToString(CultureInfo.InvariantCulture) + "\t" + tap.Due.ToString(CultureInfo.InvariantCulture)
        + "\t" + (tap.Committed ? "1" : "0") + "\n";
    private static TapReceipt? ReadTap(string[] p)
        => p.Length >= 5 && !string.IsNullOrEmpty(p[1]) && !string.IsNullOrEmpty(p[2])
            && int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount) && amount > 0
            && long.TryParse(p[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var due) && due > 0
            ? new TapReceipt { StationId = p[1], Output = p[2], Amount = amount, Due = due, Committed = p.Length > 5 && p[5] == "1" } : null;

    private static bool CanRecoverTap(TapReceipt receipt, int content, long now)
        => now >= receipt.Due && (receipt.Committed || content == 0);
    private static bool FinishTapReceipt(Ledger ledger, TapReceipt receipt)
    {
        if (!ledger.Taps.Remove(receipt)) return false;
        CreditFinished(ledger, receipt.Output, receipt.Amount);
        return true;
    }
}
