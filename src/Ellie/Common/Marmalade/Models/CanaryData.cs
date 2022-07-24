namespace Ellie.Marmalade;

public sealed record CanaryInfo(
    string Name,
    CanaryInfo? Parent,
    Canary Instance,
    IReadOnlyCollection<CanaryCommandData> Commands,
    IReadOnlyCollection<FilterAttribute> Filters)
{
    public List<CanaryInfo> Subcanaries { get; set; } = new();
}