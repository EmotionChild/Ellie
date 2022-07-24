#nullable disable
namespace Ellie.Common;

public interface ICloneable<T>
    where T : new()
{
    public T Clone();
}