#nullable disable
using CommandLine;

namespace Ellie.Modules.Help.Common;

public class CommandsOptions : IEllieCommandOptions
{
    public enum ViewType
    {
        Hide,
        Cross,
        All
    }

    [Option('v',
        "view",
        Required = false,
        Default = ViewType.Hide,
        HelpText =
            "Specifies how to output the list of commands. 0 - Hide commands which you can't use, 1 - Cross out commands which you can't use, 2 - Show all.")]
    public ViewType View { get; set; } = ViewType.Hide;

    public void NormalizeOptions()
    {
    }
}