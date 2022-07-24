#nullable disable
namespace Ellie.Modules.Games.Common.ChatterBot;

public interface IChatterBotSession
{
    Task<string> Think(string input);
}