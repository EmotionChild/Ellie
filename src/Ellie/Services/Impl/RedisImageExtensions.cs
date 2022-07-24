#nullable disable
namespace Ellie.Services;

public static class RedisImageExtensions
{
    private const string OLD_CDN_URL = "ellie.nyc3.digitaloceanspaces.com";
    private const string NEW_CDN_URL = "cdn.elliebot.net";

    public static Uri ToNewCdn(this Uri uri)
        => new(uri.ToString().Replace(OLD_CDN_URL, NEW_CDN_URL));
}