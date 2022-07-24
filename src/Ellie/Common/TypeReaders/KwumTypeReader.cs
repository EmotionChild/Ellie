#nullable disable
namespace Ellie.Common.TypeReaders;

public sealed class KwumTypeReader : EllieTypeReader<kwum>
{
    public override ValueTask<TypeReaderResult<kwum>> ReadAsync(ICommandContext context, string input)
    {
        if (kwum.TryParse(input, out var val))
            return new(TypeReaderResult.FromSuccess(val));

        return new(TypeReaderResult.FromError<kwum>(CommandError.ParseFailed, "Input is not a valid kwum"));
    }
}

public sealed class SmartTextTypeReader : EllieTypeReader<SmartText>
{
    public override ValueTask<TypeReaderResult<SmartText>> ReadAsync(ICommandContext ctx, string input)
        => new(TypeReaderResult.FromSuccess(SmartText.CreateFrom(input)));
}