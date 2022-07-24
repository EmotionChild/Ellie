#nullable disable
using System.Collections;
using System.Collections.Immutable;

namespace Ellie.Services;

public class StartingGuildsService : IEnumerable<ulong>, IEService
{
    private readonly ImmutableList<ulong> _guilds;

    public StartingGuildsService(DiscordSocketClient client)
        => _guilds = client.Guilds.Select(x => x.Id).ToImmutableList();

    public IEnumerator<ulong> GetEnumerator()
        => _guilds.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _guilds.GetEnumerator();
}