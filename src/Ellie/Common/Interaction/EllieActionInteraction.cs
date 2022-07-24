namespace Ellie;

public sealed class EllieButtonActionInteraction : EllieButtonOwnInteraction
{
    private readonly EllieInteractionData _data;
    private readonly Func<SocketMessageComponent, Task> _action;

    public EllieButtonActionInteraction(
        DiscordSocketClient client,
        ulong authorId,
        EllieInteractionData data,
        Func<SocketMessageComponent, Task> action
    )
        : base(client, authorId)
    {
        _data = data;
        _action = action;
    }

    protected override string Name
        => _data.CustomId;
    protected override IEmote Emote
        => _data.Emote;
    protected override string? Text
        => _data.Text;

    public override Task ExecuteOnActionAsync(SocketMessageComponent smc)
        => _action(smc);
}