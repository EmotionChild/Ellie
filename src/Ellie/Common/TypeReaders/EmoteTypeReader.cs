#nullable disable
namespace Ellie.Common.TypeReaders;

public sealed class EmoteTypeReader : EllieTypeReader<Emote>
{
    public override ValueTask<TypeReaderResult<Emote>> ReadAsync(ICommandContext ctx, string input)
    {
        if (!Emote.TryParse(input, out var emote))
            return new(TypeReaderResult.FromError<Emote>(CommandError.ParseFailed, "Input is not a valid emote"));

        return new(TypeReaderResult.FromSuccess(emote));
    }
}