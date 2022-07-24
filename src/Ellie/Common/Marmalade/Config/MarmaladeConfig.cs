#nullable enable
using Cloneable;
using Ellie.Common.Yml;

namespace Ellie.Marmalade;

[Cloneable]
public sealed partial class MarmaladeConfig : ICloneable<MarmaladeConfig>
{
    [Comment(@"DO NOT CHANGE")]
    public int Version { get; set; } = 1;
    
    [Comment("List of marmalades automatically loaded at startup")]
    public List<string>? Loaded { get; set; }

    public MarmaladeConfig()
    {
        Loaded = new();
    }
}