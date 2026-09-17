using System.Collections.Generic;

namespace RestlessCook;

public sealed class CookUse
{
    public string Item { get; set; } = "";
    public int Amount { get; set; }
}

public sealed class CookRow
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Source { get; set; } = "";
    public string Operation { get; set; } = "";
    public string Tier { get; set; } = "";
    public string Prefab { get; set; } = "";
    public string RecipeId { get; set; } = "";
    public string CloneFrom { get; set; } = "";
    public List<string> Feeds { get; } = new();
    public List<CookUse> Uses { get; } = new();
    public int OutputAmount { get; set; } = 1;
    public string Station { get; set; } = "";
    public int StationLevel { get; set; } = 1;

    public bool IsAdd => Operation == "add";
    public bool IsRewrite => Operation == "rewrite";
    public bool IsReference => Operation == "reference";
    public bool IsMeal => Kind == "meal";
    public bool IsFeast => Kind == "feast";
    public bool IsSideboard => Kind == "sideboard";
}
