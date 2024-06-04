namespace library;

public class ContainerStats
{
    public long TotalCapacity { get; set; }
    public long UsedCapacity { get; set; }
    public long RemainingCapacity { get; set; }
    public long TotalFiles { get; set; }
    public long TotalDirectories { get; set; }
    public Dictionary<string, long> SizePerDirectory { get; set; }
}