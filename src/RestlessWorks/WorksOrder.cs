namespace RestlessWorks;

public enum WorksOrderMode
{
    Make,
    Keep
}

public sealed class WorksOrder
{
    public int Id;
    public string Output = "";
    public string Name = "";
    public WorksOrderMode Mode;
    public int Count;
    public long PlayerId;
    public int Ready;
    public int Collected;
    public int InProduction;
    public int Available;
    public int Remaining;
}

