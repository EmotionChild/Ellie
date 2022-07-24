#nullable enable

[DontAddToIocContainer]
public sealed class BehaviorAdapter : ICustomBehavior
{
    private readonly WeakReference<Canary> _canaryWr;
    private readonly IMarmaladeStrings _strings;
    private readonly IServiceProvider _services;
    private readonly string _name;

    // unused
    public int Priority
        => 0;

    public BehaviorAdapter(WeakReference<Canary> canaryWr, IMarmaladeStrings strings, IServiceProvider services)
    {
        _canaryWr = canaryWr;
        _strings = strings;
        _services = services;

        _name = canaryWr.TryGetTarget(out var canary)
            ? $"canary/{canary.GetType().Name}"
            : "unknown";
    }

    public async Task<bool> ExecPreCommandAsync(ICommandContext context, string moduleName, CommandInfo command)
    {
        if (!_canaryWr.TryGetTarget(out var canary))
            return false;

        return await canary.ExecPreCommandAsync(ContextAdapterFactory.CreateNew(context, _strings, _services),
            moduleName,
            command.Name);
    }

    public async Task<bool> ExecOnMessageAsync(IGuild? guild, IUserMessage msg)
    {
        if (!_canaryWr.TryGetTarget(out var canary))
            return false;

        return await canary.ExecOnMessageAsync(guild, msg);
    }

    public async Task<string?> TransformInput(
        IGuild guild,
        IMessageChannel channel,
        IUser user,
        string input)
    {
        if (!_canaryWr.TryGetTarget(out var canary))
            return null;
        
        return await canary.ExecInputTransformAsync(guild, channel, user, input);
    }

    public async Task ExecOnNoCommandAsync(IGuild? guild, IUserMessage msg)
    {
        if (!_canaryWr.TryGetTarget(out var canary))
            return;

        await canary.ExecOnNoCommandAsync(guild, msg);
    }

    public async ValueTask ExecPostCommandAsync(ICommandContext context, string moduleName, string commandName)
    {
        if (!_canaryWr.TryGetTarget(out var canary))
            return;
        
        await canary.ExecPostCommandAsync(ContextAdapterFactory.CreateNew(context, _strings, _services),
            moduleName,
            commandName);
    }

    public override string ToString()
        => _name;
}