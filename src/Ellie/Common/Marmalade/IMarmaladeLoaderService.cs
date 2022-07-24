using System.Globalization;

namespace Ellie.Marmalade;

public interface IMarmaladeLoaderService
{
    Task<MarmaladeLoadResult> LoadMarmaladeAsync(string marmaladeName);
    Task<MarmaladeUnloadResult> UnloadMarmaladeAsync(string marmaladeName);
    string GetCommandDescription(string marmaladeName, string commandName, CultureInfo culture);
    string[] GetCommandExampleArgs(string marmaladeName, string commandName, CultureInfo culture);
    Task ReloadStrings();
    IReadOnlyCollection<string> GetAllMarmalades();
    IReadOnlyCollection<MarmaladeStats> GetLoadedMarmalades(CultureInfo? cultureInfo = null);
}

public sealed record MarmaladeStats(string Name,
    string? Description,
    IReadOnlyCollection<CanaryStats> Canaries);
    
public sealed record CanaryStats(string Name, 
    IReadOnlyCollection<CanaryCommandStats> Commands);

public sealed record CanaryCommandStats(string Name);