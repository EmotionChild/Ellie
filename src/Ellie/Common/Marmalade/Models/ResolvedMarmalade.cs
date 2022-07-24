using System.Collections.Immutable;

namespace Ellie.Marmalade;

public sealed record ResolvedMarmalade(
    WeakReference<MarmaladeAssemblyLoadContext> LoadContext,
    IImmutableList<ModuleInfo> ModuleInfos,
    IImmutableList<CanaryInfo> CanaryInfos,
    IMarmaladeStrings Strings,
    Dictionary<Type, TypeReader> TypeReaders,
    IReadOnlyCollection<ICustomBehavior> Execs)
{
    public IServiceProvider Services { get; set; } = null!;
}