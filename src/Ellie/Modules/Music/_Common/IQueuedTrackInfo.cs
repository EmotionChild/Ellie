#nullable disable
namespace Ellie.Modules.Music;

public interface IQueuedTrackInfo : ITrackInfo
{
    public ITrackInfo TrackInfo { get; }

    public string Queuer { get; }
}