namespace RestlessDrawers;

internal sealed class DrawerRow
{
    public string Id = "";
    public string Prefab = "";
    public string Source = "";
    public string Name = "";
    public string Description = "";
}

internal static class DrawerBook
{
    public static readonly DrawerRow[] Rows =
    {
        new()
        {
            Id = "wooden",
            Prefab = "RestlessDrawerWood",
            Source = "piece_chest_wood",
            Name = "Wood drawer",
            Description = "A furniture chest. Same space as a wood chest."
        },
        new()
        {
            Id = "personal",
            Prefab = "RestlessDrawerPersonal",
            Source = "piece_chest_private",
            Name = "Personal drawer",
            Description = "A furniture chest only you can open. Same space as a personal chest."
        },
        new()
        {
            Id = "reinforced",
            Prefab = "RestlessDrawerReinforced",
            Source = "piece_chest",
            Name = "Reinforced drawer",
            Description = "A furniture chest. Same space as a reinforced chest."
        },
        new()
        {
            Id = "blackmetal",
            Prefab = "RestlessDrawerBlackMetal",
            Source = "piece_chest_blackmetal",
            Name = "Black metal drawer",
            Description = "A furniture chest. Same space as a black metal chest."
        }
    };

    public static bool IsOurs(string? prefab)
    {
        var name = prefab ?? "";
        return name.Length > 0 && name.StartsWith("RestlessDrawer", System.StringComparison.Ordinal);
    }
}
