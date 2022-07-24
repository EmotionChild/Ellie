#nullable disable
using Ellie.Modules.Gambling.Common.AnimalRacing;

namespace Ellie.Modules.Gambling.Services;

public class AnimalRaceService : IEService
{
    public ConcurrentDictionary<ulong, AnimalRace> AnimalRaces { get; } = new();
}