namespace Ellie;

/// <summary>
/// Builder class for EllieInteractions
/// </summary>
public class EllieInteractionBuilder
{
    private EllieInteractionData? iData;
    private Func<SocketMessageComponent, Task>? action;
    // private bool isOwn;

    public EllieInteractionBuilder WithData<T>(in T data)
        where T : EllieInteractionData
    {
        iData = data;
        return this;
    }

    // public EllieOwnInteractionBuiler WithIsOwn(bool isOwn = true)
    // {
    //     this.isOwn = isOwn;
    //     return this;
    
    // }
    
    public EllieInteractionBuilder WithAction(in Func<SocketMessageComponent, Task> fn)
    {
        this.action = fn;
        return this;
    }

    public EllieButtonActionInteraction Build(DiscordSocketClient client, ulong userId)
    {
        if (iData is null)
            throw new InvalidOperationException("You have to specify the data before building the interaction");

        if (action is null)
            throw new InvalidOperationException("You have to specify the action before building the interaction");

        return new(client, userId, iData, action);
    }
}