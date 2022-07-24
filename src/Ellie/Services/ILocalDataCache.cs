#nullable disable
using Ellie.Common.Pokemon;
using Ellie.Modules.Games.Common.Trivia;

namespace Ellie.Services;

public interface ILocalDataCache
{
    Task<IReadOnlyDictionary<string, SearchPokemon>> GetPokemonsAsync();
    Task<IReadOnlyDictionary<string, SearchPokemonAbility>> GetPokemonAbilitiesAsync();
    Task<TriviaQuestionModel[]> GetTriviaQuestionsAsync();
    Task<IReadOnlyDictionary<int, string>> GetPokemonMapAsync();
}