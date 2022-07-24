#nullable disable
using Ellie.Modules.Gambling.Common.Blackjack;

namespace Ellie.Modules.Gambling.Services;

public class BlackJackService : IEService
{
    public ConcurrentDictionary<ulong, Blackjack> Games { get; } = new();
}