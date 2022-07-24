using System.Runtime.CompilerServices;

namespace Ellie.Marmalade;

public class MarmaladeServiceProvider : IServiceProvider
{
    private readonly IServiceProvider _ellieServices;
    private readonly IServiceProvider _marmaladeServices;

    public MarmaladeServiceProvider(IServiceProvider ellieServices, IServiceProvider marmaladeServices)
    {
        _ellieServices = ellieServices;
        _marmaladeServices = marmaladeServices;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public object? GetService(Type serviceType)
    {
        if (!serviceType.Assembly.IsCollectible)
            return _ellieServices.GetService(serviceType);

        return _marmaladeServices.GetService(serviceType);
    }
}