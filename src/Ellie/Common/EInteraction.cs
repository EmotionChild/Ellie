namespace Ellie.Common;

public abstract class EInteraction
{
    private readonly DiscordSocketClient _client;
    private readonly ulong _userId;
    private readonly Func<SocketMessageComponent, Task> _action;

    protected abstract EllieInteractionData Data { get; }

    public EInteraction(
        DiscordSocketClient client,
        ulong userId,
        Func<SocketMessageComponent, Task> action)
    {
        _client = client;
        _userId = userId;
        _action = action;
    }

    public EllieButtonInteraction GetInteraction()
        => new EllieInteractionBuilder()
           .WithData(Data)
           .WithAction(_action)
           .Build(_client, _userId);
}