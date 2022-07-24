#nullable disable
namespace Ellie.Modules.Music;

public interface ISoundcloudResolver : IPlatformQueryResolver
{
    bool IsSoundCloudLink(string url);
    IAsyncEnumerable<ITrackInfo> ResolvePlaylistAsync(string playlist);
}