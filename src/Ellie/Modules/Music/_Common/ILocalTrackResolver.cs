#nullable disable
namespace Ellie.Modules.Music;

public interface ILocalTrackResolver : IPlatformQueryResolver
{
    IAsyncEnumerable<ITrackInfo> ResolveDirectoryAsync(string dirPath);
}