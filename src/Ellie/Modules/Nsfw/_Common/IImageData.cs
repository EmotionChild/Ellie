#nullable disable
namespace Ellie.Modules.Nsfw.Common;

public interface IImageData
{
    ImageData ToCachedImageData(Booru type);
}