namespace Ellie.Marmalade;

public interface IMarmaladeConfigService
{
    IReadOnlyCollection<string> GetLoadedMarmalades();
    void AddLoadedMarmalade(string name);
    void RemoveLoadedMarmalade(string name);
}