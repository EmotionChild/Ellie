#nullable disable
using System.Diagnostics.CodeAnalysis;

namespace Ellie.Common;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
[SuppressMessage("Style", "IDE0022:Use expression body for methods")]
public sealed class NoPublicBotAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(
        ICommandContext context,
        CommandInfo command,
        IServiceProvider services)
    {
#if GLOBAL_ELLIE
        return Task.FromResult(PreconditionResult.FromError("Not available on the public bot. To learn how to selfhost a private bot, click [here](https://docs.elliebot.net)."));
#else
        return Task.FromResult(PreconditionResult.FromSuccess());
#endif
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
[SuppressMessage("Style", "IDE0022:Use expression body for methods")]
public sealed class OnlyPublicBotAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(
        ICommandContext context,
        CommandInfo command,
        IServiceProvider services)
    {
#if GLOBAL_ELLIE || DEBUG
        return Task.FromResult(PreconditionResult.FromSuccess());
#else
        return Task.FromResult(PreconditionResult.FromError("Only available on the public bot."));
#endif
    }
}