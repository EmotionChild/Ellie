#nullable disable
using System.Runtime.InteropServices;

namespace Ellie.Modules.Permissions;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct CleverBotResponseStr
{
    public const string CLEVERBOT_RESPONSE = "cleverbot:response";
}