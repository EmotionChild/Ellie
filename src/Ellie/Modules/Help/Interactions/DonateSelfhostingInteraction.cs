namespace Ellie.Modules.Help;

public class DonateSelfhostingInteraction : EInteraction
{
    protected override EllieInteractionData Data
        => new EllieInteractionData(new Emoji("🖥️"), "donate:selfhosting", "Selfhosting");
    
    public DonateSelfhostingInteraction(DiscordSocketClient client, ulong userId, Func<SocketMessageComponent, Task> action)
        : base(client, userId, action)
    {
    }
}