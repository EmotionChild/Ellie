#nullable disable
using Ellie.Db.Models;
using System.Text.Json.Serialization;

namespace Ellie.Modules.Searches.Common;

public readonly struct StreamDataKey
{
    public FollowedStream.FType Type { get; init; }
    public string Name { get; init; }

    public StreamDataKey(FollowedStream.FType type, string name)
    {
        Type = type;
        Name = name;
    }
}