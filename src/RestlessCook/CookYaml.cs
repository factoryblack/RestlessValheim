using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RestlessCook;

public static class CookYaml
{
    public static CookBook LoadEmbedded()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("RestlessCook.cook.yaml")
            ?? throw new InvalidOperationException("Missing embedded cook.yaml");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    public static CookBook Parse(string text)
    {
        var book = new CookBook();
        CookRow? cur = null;
        CookUse? use = null;
        string? mode = null;
        string? section = null;

        void Flush()
        {
            if (cur == null)
                return;
            if (section == "kit")
                book.Kit.Add(cur);
            else if (section == "items")
                book.Items.Add(cur);
            cur = null;
        }

        foreach (var raw in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            var line = raw.TrimEnd();
            if (line == "kit:" || line == "items:")
            {
                Flush();
                section = line.TrimEnd(':');
                use = null;
                mode = null;
                continue;
            }

            if (section == null)
                continue;

            if (line.StartsWith("  - id:", StringComparison.Ordinal))
            {
                Flush();
                cur = new CookRow { Id = Value(line) };
                use = null;
                mode = null;
                continue;
            }

            if (cur == null)
                continue;

            if (line.StartsWith("    feeds:", StringComparison.Ordinal))
            {
                mode = "feeds";
                if (line.Contains("[]"))
                    mode = null;
                continue;
            }

            if (line.StartsWith("    uses:", StringComparison.Ordinal))
            {
                mode = "uses";
                continue;
            }

            if (mode == "feeds" && line.StartsWith("      - ", StringComparison.Ordinal))
            {
                cur.Feeds.Add(line.Substring(line.IndexOf('-') + 1).Trim());
                continue;
            }

            if (mode == "uses" && line.StartsWith("      - item:", StringComparison.Ordinal))
            {
                use = new CookUse { Item = Value(line) };
                cur.Uses.Add(use);
                continue;
            }

            if (mode == "uses" && use != null && line.StartsWith("        amount:", StringComparison.Ordinal))
            {
                use.Amount = int.Parse(Value(line));
                continue;
            }

            if (line.StartsWith("    ", StringComparison.Ordinal) && line.Contains(":") && !line.StartsWith("      ", StringComparison.Ordinal))
            {
                mode = null;
                var idx = line.IndexOf(':');
                var key = line.Substring(0, idx).Trim();
                var val = Unquote(line.Substring(idx + 1).Trim());
                switch (key)
                {
                    case "name": cur.Name = val; break;
                    case "kind": cur.Kind = val; break;
                    case "source": cur.Source = val; break;
                    case "operation": cur.Operation = val; break;
                    case "tier": cur.Tier = val; break;
                    case "prefab": cur.Prefab = val; break;
                    case "recipe_id": cur.RecipeId = val; break;
                    case "clone_from": cur.CloneFrom = val; break;
                    case "food": cur.Food = int.Parse(val); break;
                    case "food_stamina": cur.FoodStamina = int.Parse(val); break;
                    case "food_eitr": cur.FoodEitr = int.Parse(val); break;
                    case "food_regen": cur.FoodRegen = float.Parse(val, System.Globalization.CultureInfo.InvariantCulture); break;
                    case "food_minutes": cur.FoodMinutes = int.Parse(val); break;
                    case "output_amount": cur.OutputAmount = int.Parse(val); break;
                    case "station": cur.Station = val; break;
                    case "station_level": cur.StationLevel = int.Parse(val); break;
                }
            }
        }

        Flush();
        Validate(book.Items);
        ValidateKit(book.Kit);
        return book;
    }

    public static void Validate(IReadOnlyList<CookRow> items)
    {
        if (items.Count != 81)
            throw new InvalidOperationException($"cook.yaml must have 81 rows, got {items.Count}");

        var ids = new HashSet<string>(items.Select(i => i.Id), StringComparer.Ordinal);
        if (ids.Count != 81)
            throw new InvalidOperationException("cook.yaml has duplicate ids");

        foreach (var row in items)
        {
            if (row.Uses.Count == 0)
                throw new InvalidOperationException($"{row.Id}: empty uses[]");
            foreach (var feed in row.Feeds)
            {
                if (!ids.Contains(feed))
                    throw new InvalidOperationException($"{row.Id}: feeds unknown id {feed}");
            }
        }

        int Count(Func<CookRow, bool> pred) => items.Count(pred);
        Require(Count(i => i.IsAdd) == 35, "35 add rows");
        Require(Count(i => i.IsRewrite) == 16, "16 rewrite rows");
        Require(Count(i => i.IsReference) == 30, "30 reference rows");
        Require(Count(i => i.IsAdd || i.IsRewrite) == 51, "51 recipe operations");
        Require(Count(i => i.IsMeal && i.Source == "custom" && i.IsAdd) == 17, "17 custom meals");
        Require(Count(i => i.IsFeast && i.Source == "custom" && i.IsAdd) == 16, "16 custom feasts");
        Require(Count(i => i.IsSideboard && i.IsAdd) == 2, "2 sideboards");
        Require(Count(i => i.IsMeal && i.IsRewrite) == 14, "14 cooked-first rewrites");
        Require(Count(i => i.IsFeast && i.IsRewrite) == 2, "2 feast reroutes");
        Require(Count(i => i.IsMeal && i.IsReference) == 24, "24 meal references");
        Require(Count(i => i.IsFeast && i.IsReference) == 6, "6 feast references");

        foreach (var row in items.Where(i => i.IsAdd))
        {
            if (string.IsNullOrEmpty(row.CloneFrom))
                throw new InvalidOperationException($"{row.Id}: add requires clone_from");
            if (string.IsNullOrEmpty(row.Prefab))
                throw new InvalidOperationException($"{row.Id}: add requires prefab");
        }

        foreach (var row in items.Where(i => i.IsAdd && (i.IsMeal || i.IsFeast)))
        {
            if (row.Food == 0 && row.FoodStamina == 0 && row.FoodEitr == 0)
                throw new InvalidOperationException($"{row.Id}: custom meal/feast needs food stats");
            if (row.FoodMinutes <= 0)
                throw new InvalidOperationException($"{row.Id}: custom meal/feast needs food_minutes");
            if (row.FoodRegen <= 0f)
                throw new InvalidOperationException($"{row.Id}: custom meal/feast needs food_regen");
        }

        foreach (var row in items.Where(i => i.IsSideboard))
        {
            if (row.Food != 0 || row.FoodStamina != 0 || row.FoodEitr != 0 || row.FoodMinutes != 0)
                throw new InvalidOperationException($"{row.Id}: sideboard is not edible");
        }

        foreach (var row in items.Where(i => i.IsRewrite))
        {
            if (string.IsNullOrEmpty(row.RecipeId))
                throw new InvalidOperationException($"{row.Id}: rewrite requires recipe_id");
        }

        var byId = items.ToDictionary(i => i.Id, StringComparer.Ordinal);
        foreach (var meal in items.Where(i => i.IsMeal))
        {
            if (!ReachesFeast(meal, byId))
                throw new InvalidOperationException($"{meal.Id}: meal has no path to a feast");
        }

        foreach (var row in items.Where(i => i.IsFeast && i.Source == "custom" && i.IsAdd && (i.Tier == "meadows" || i.Tier == "blackforest")))
        {
            if (row.Uses.Any(u => u.Item == "SpiceForests"))
                throw new InvalidOperationException($"{row.Id}: early custom feast cannot require Bog Witch spices");
        }
    }

    private static void ValidateKit(IReadOnlyList<CookRow> kit)
    {
        if (kit.Count != 2)
            throw new InvalidOperationException($"cook.yaml kit must have 2 rows, got {kit.Count}");
        var table = kit.FirstOrDefault(i => i.Id == "food_preparation_table");
        var tray = kit.FirstOrDefault(i => i.Id == "serving_tray");
        if (table == null || !table.IsStation || !table.IsRewrite || table.Prefab != "piece_preptable" || table.Uses.Count == 0)
            throw new InvalidOperationException("kit: food_preparation_table must rewrite piece_preptable");
        if (tray == null || !tray.IsTool || !tray.IsAdd || tray.Prefab != "Feaster" || tray.Uses.Count == 0)
            throw new InvalidOperationException("kit: serving_tray must add a Feaster recipe");
    }

    private static bool ReachesFeast(CookRow start, IReadOnlyDictionary<string, CookRow> byId)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<string>();
        stack.Push(start.Id);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (!seen.Add(id))
                continue;
            if (!byId.TryGetValue(id, out var row))
                continue;
            if (row.IsFeast && id != start.Id)
                return true;
            foreach (var feed in row.Feeds)
                stack.Push(feed);
        }

        return start.IsFeast;
    }

    private static void Require(bool ok, string label)
    {
        if (!ok)
            throw new InvalidOperationException($"cook.yaml failed check: {label}");
    }

    private static string Value(string line)
    {
        var idx = line.IndexOf(':');
        return Unquote(line.Substring(idx + 1).Trim());
    }

    private static string Unquote(string val)
    {
        if (val.Length >= 2 && ((val[0] == '"' && val[val.Length - 1] == '"') || (val[0] == '\'' && val[val.Length - 1] == '\'')))
            return val.Substring(1, val.Length - 2);
        return val;
    }
}
