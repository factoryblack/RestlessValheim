using RestlessQoL.Core;

const string header = "Set effect (3 parts): Berserk";
const string body = "Increases damage and regeneration.\nHealth regen: +30%\nSlash: +10%\nChop: +10%";
static void Check(bool ok, string name)
{
    if (!ok) throw new Exception(name);
    Console.WriteLine("PASS " + name);
}
var raw = "Armour: 13\nHealth regen: +30%\n\n" + header + "\n" + body + "\n\nCrafted by: Test\nAppendix";
Check(TooltipSetBlock.TrySplit(raw, header, body, out var item), "recognises native set block");
Check(item.Contains("Armour: 13") && item.Contains("Health regen: +30%"), "identical per-piece stat survives");
Check(!item.Contains("Slash") && !item.Contains("Chop") && !item.Contains("Berserk"), "set stats and damage leave the item scope");
Check(item.Contains("Crafted by: Test\nAppendix"), "trailing native/mod content survives");
Check(TooltipSetBlock.TrySplit("Seteffect(3 parts):Berserk\r\n\n" + body.Replace(" ", "\u00a0"), header, body, out _), "rich-text whitespace and CRLF tolerated");
Check(TooltipSetBlock.TrySplit("Ensemble (3 pièces): Rage\nTranchant: +10%", "Ensemble (3 pièces): Rage", "Tranchant: +10%", out _), "localised header and body");
Check(!TooltipSetBlock.TrySplit("Armour: 13", header, body, out item) && item == "Armour: 13", "non-set tooltip unchanged");
Check(!TooltipSetBlock.TrySplit(raw, header, body + "\nUnknown: 3", out item) && item == raw, "mismatched body is lossless");
Check(!TooltipSetBlock.TrySplit(raw, header, "", out item) && item == raw, "empty set body is lossless");
Console.WriteLine("9 set-tooltip regressions passed.");
