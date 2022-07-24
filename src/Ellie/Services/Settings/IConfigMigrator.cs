#nullable disable
namespace Ellie.Services;

public interface IConfigMigrator
{
    public void EnsureMigrated();
}