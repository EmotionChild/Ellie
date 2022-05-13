using System.Runtime.CompilerServices;

namespace Ellie.Plugin;

public class PluginServiceProvider : IServiceProvider
{
    private readonly IServiceProvider _ellieServices;
    private readonly IServiceProvider _pluginServices;

    public PluginServiceProvider(IServiceProvider ellieServices, IServiceProvider pluginServices)
    {
        _ellieServices = ellieServices;
        _pluginServices = pluginServices;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public object? GetService(Type serviceType)
    {
        if (!serviceType.Assembly.IsCollectible)
            return _ellieServices.GetService(serviceType);

        return _pluginServices.GetService(serviceType);
    }
}