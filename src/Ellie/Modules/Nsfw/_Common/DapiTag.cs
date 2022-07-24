#nullable disable
using System.Text.Json.Serialization;

namespace Ellie.Modules.Nsfw.Common;

public readonly struct DapiTag
{
    public string Name { get; }

    [JsonConstructor]
    public DapiTag(string name)
        => Name = name;
}