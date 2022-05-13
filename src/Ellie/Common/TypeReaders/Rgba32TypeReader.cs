using Color = SixLabors.ImageSharp.Color;

#nullable disable
namespace Ellie.Common.TypeReaders;

public sealed class Rgba32TypeReader : EllieTypeReader<Color>
{
    public override ValueTask<TypeReaderResult<Color>> ReadAsync(ICommandContext context, string input)
    {
        input = input.Replace("#", "", StringComparison.InvariantCulture);
        try
        {
            return ValueTask.FromResult(TypeReaderResult.FromSuccess(Color.ParseHex(input)));
        }
        catch
        {
            return ValueTask.FromResult(TypeReaderResult.FromError<Color>(CommandError.ParseFailed, "Parameter is not a valid color hex"));
        }
    }
}