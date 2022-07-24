namespace Ellie.Modules.Searches.Youtube;

public sealed class YtdlpYoutubeSearchService : YoutubedlxServiceBase, IEService
{
    public override async Task<VideoInfo?> SearchAsync(string query)
        => await InternalGetInfoAsync(query, true);
}