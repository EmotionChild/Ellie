using System.Reflection;
using System.Runtime.Loader;

namespace Ellie.Marmalade;

public sealed class MarmaladeAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _depResolver;

    public MarmaladeAssemblyLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _depResolver = new(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _depResolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _depResolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}