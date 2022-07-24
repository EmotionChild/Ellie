using System.Diagnostics.CodeAnalysis;

namespace Ellie.Modules.Games.Hangman;

public interface IHangmanSource : IEService
{
    public IReadOnlyCollection<string> GetCategories();
    public void Reload();
    public bool GetTerm(string? category, [NotNullWhen(true)] out HangmanTerm? term);
}