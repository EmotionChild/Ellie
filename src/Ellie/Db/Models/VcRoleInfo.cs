#nullable disable
namespace Ellie.Services.Database.Models;

public class VcRoleInfo : DbEntity
{
    public ulong VoiceChannelId { get; set; }
    public ulong RoleId { get; set; }
}