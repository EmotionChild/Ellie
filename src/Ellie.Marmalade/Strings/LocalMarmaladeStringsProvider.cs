namespace Ellie.Bird;

public class LocalMarmaladeStringsProvider : IMarmaladeStringsProvider
{
    private readonly StringsLoader _source;
    private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _responseStrings;
    private IReadOnlyDictionary<string, IReadOnlyDictionary<string, CommandStrings>> _commandStrings;

    public LocalMarmaladeStringsProvider(StringsLoader source)
    {
        _source = source;
        _responseStrings = _source.GetResponseStrings();
        _commandStrings = _source.GetCommandStrings();
    }

    public void Reload()
    {
        _responseStrings = _source.GetResponseStrings();
        _commandStrings = _source.GetCommandStrings();
    }


    public string? GetText(string localeName, string key)
    {
        if (_responseStrings.TryGetValue(localeName.ToLowerInvariant(), out var langStrings)
            && langStrings.TryGetValue(key.ToLowerInvariant(), out var text))
            return text;

        return null;
    }

    public CommandStrings? GetCommandStrings(string localeName, string commandName)
    {
        if (_commandStrings.TryGetValue(localeName.ToLowerInvariant(), out var langStrings)
            && langStrings.TryGetValue(commandName.ToLowerInvariant(), out var strings))
            return strings;

        return null;
    }
}