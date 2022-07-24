#nullable disable
using CommandLine;

namespace Ellie.Common;

public class LbOpts : IEllieCommandOptions
{
    [Option('c', "clean", Default = false, HelpText = "Only show users who are on the server.")]
    public bool Clean { get; set; }

    public void NormalizeOptions()
    {
    }
}