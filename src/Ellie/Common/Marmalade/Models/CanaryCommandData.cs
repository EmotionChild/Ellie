using Microsoft.VisualBasic;
using System.Reflection;
using CommandStrings = Ellie.Marmalade.CommandStrings;

namespace Ellie.Marmalade;

public sealed class CanaryCommandData
{
    public CanaryCommandData(
        IReadOnlyCollection<string> aliases,
        MethodInfo methodInfo,
        Canary module,
        FilterAttribute[] filters,
        CommandContextType contextType,
        IReadOnlyList<Type> injectedParams,
        IReadOnlyList<ParamData> parameters,
        CommandStrings strings,
        int priority)
    {
        Aliases = aliases;
        MethodInfo = methodInfo;
        Module = module;
        Filters = filters;
        ContextType = contextType;
        InjectedParams = injectedParams;
        Parameters = parameters;
        Priority = priority;
        OptionalStrings = strings;
    }

    public CommandStrings OptionalStrings { get; set; }

    public IReadOnlyCollection<string> Aliases { get; }
    public MethodInfo MethodInfo { get; set; }
    public Canary Module { get; set; }
    public FilterAttribute[] Filters { get; set; }
    public CommandContextType ContextType { get; }
    public IReadOnlyList<Type> InjectedParams { get; }
    public IReadOnlyList<ParamData> Parameters { get; }
    public int Priority { get; }
}