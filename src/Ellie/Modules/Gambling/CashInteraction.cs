#nullable disable
namespace Ellie.Modules.Gambling;

public class CashInteraction : EInteraction
{
    protected override EllieInteractionData Data
        => new EllieInteractionData(new Emoji("🏦"), "cash:bank_show_balance");

    public CashInteraction(DiscordSocketClient client, ulong userId, Func<SocketMessageComponent, Task> action)
        : base(client, userId, action)
    {
    }
}