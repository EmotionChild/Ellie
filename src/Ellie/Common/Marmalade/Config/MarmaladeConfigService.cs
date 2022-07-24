using Ellie.Common.Configs;

namespace Ellie.Marmalade;

public sealed class MarmaladeConfigService : ConfigServiceBase<MarmaladeConfig>, IMarmaladeConfigService
{
    private const string FILE_PATH = "data/marmalades/medusa.yml";
    private static readonly TypedKey<MarmaladeConfig> _changeKey = new("config.medusa.updated");

    public override string Name
        => "medusa";

    public MarmaladeConfigService(
        IConfigSeria serializer,
        IPubSub pubSub)
        : base(FILE_PATH, serializer, pubSub, _changeKey)
    {
    }

    public IReadOnlyCollection<string> GetLoadedMarmalades()
        => Data.Loaded?.ToList() ?? new List<string>();

    public void AddLoadedMarmalade(string name)
    {
        name = name.Trim().ToLowerInvariant();

        ModifyConfig(conf =>
        {
            if (conf.Loaded is null)
                conf.Loaded = new();

            if (!conf.Loaded.Contains(name))
                conf.Loaded.Add(name);
        });
    }

    public void RemoveLoadedMarmalade(string name)
    {
        name = name.Trim().ToLowerInvariant();

        ModifyConfig(conf =>
        {
            if (conf.Loaded is null)
                conf.Loaded = new();

            conf.Loaded.Remove(name);
        });
    }
}