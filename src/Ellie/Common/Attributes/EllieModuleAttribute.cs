using System.Runtime.CompilerServices;

namespace Ellie.Common.Attributes;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class EllieModuleAttribute : GroupAttribute
{
    public EllieModuleAttribute(string moduleName)
        : base(moduleName)
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class EllieDescriptionAttribute : SummaryAttribute
{
    public EllieDescriptionAttribute([CallerMemberName] string name = "")
        : base(name.ToLowerInvariant())
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class EllieUsageAttribute : RemarksAttribute
{
    public EllieUsageAttribute([CallerMemberName] string name = "")
        : base(name.ToLowerInvariant())
    {
    }
}