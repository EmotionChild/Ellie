#nullable disable
using Ellie.Common.TypeReaders;
using static Ellie.Common.TypeReaders.TypeReaderResult;

namespace Ellie.Modules.Permissions;

public class CleverbotResponseCmdCdTypeReader : EllieTypeReader<CleverBotResponseStr>
{
    public override ValueTask<TypeReaderResult<CleverBotResponseStr>> ReadAsync(
        ICommandContext ctx,
        string input)
        => input.ToLowerInvariant() == CleverBotResponseStr.CLEVERBOT_RESPONSE
            ? new(FromSuccess(new CleverBotResponseStr()))
            : new(FromError<CleverBotResponseStr>(CommandError.ParseFailed, "Not a valid cleverbot"));
}