namespace Ellie.Modules.Searches.Youtube;

public sealed class YtdlYoutubeSearchService : YoutubedlxServiceBase, IEService
{
    public override async Task<VideoInfo?> SearchAsync(string query)
        => await InternalGetInfoAsync(query, false);
}