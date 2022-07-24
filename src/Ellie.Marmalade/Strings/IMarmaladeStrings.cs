using System.Globalization;

namespace Ellie.Marmalade;

/// <summary>
///     Defines methods to retrieve and reload marmalade strings
/// </summary>
public interface IMarmaladeStrings
{
    // string GetText(string key, ulong? guildId = null, params object[] data);
    string? GetText(string key, CultureInfo locale, params object[] data);
    void Reload();
    CommandStrings GetCommandStrings(string commandName, CultureInfo cultureInfo);
    string? GetDescription(CultureInfo? locale);
}