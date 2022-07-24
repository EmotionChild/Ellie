#nullable disable
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Ellie.Modules.Utility;

public partial class Utility
{
    [Group]
    public partial class EvalCommands : EllieModule
    {
        private readonly IServiceProvider _services;

        public EvalCommands(IServiceProvider services)
        {
            _services = services;
        }
        
        [Cmd]
        [NoPublicBot]
        [OwnerOnly]
        public async partial Task Eval([Leftover] string scriptText)
        {
            _ = ctx.Channel.TriggerTypingAsync();

            if (scriptText.StartsWith("```cs"))
                scriptText = scriptText[5..];
            else if (scriptText.StartsWith("```"))
                scriptText = scriptText[3..];

            if (scriptText.EndsWith("```"))
                scriptText = scriptText[..^3];
            
            var script = CSharpScript.Create(scriptText,
                ScriptOptions.Default
                             .WithReferences(this.GetType().Assembly)
                             .WithImports(
                                 "System",
                                 "Ellie",
                                 "Ellie.Extensions",
                                 "Microsoft.Extensions.DependencyInjection",
                                 "Ellie.Common",
                                 "System.Text",
                                 "System.Text.Json"),
                globalsType: typeof(EvalGlobals));
            
            try
            {
                var result = await script.RunAsync(new EvalGlobals()
                {
                    ctx = this.ctx,
                    guild = this.ctx.Guild,
                    channel = this.ctx.Channel,
                    user = this.ctx.User,
                    self = this,
                    services = _services
                });

                var output = result.ReturnValue?.ToString();
                if (!string.IsNullOrWhiteSpace(output))
                {
                    var eb = _eb.Create(ctx)
                                .WithOkColor()
                                .AddField("Code", scriptText)
                                .AddField("Output", output.TrimTo(512)!);

                    _ = ctx.Channel.EmbedAsync(eb);
                }
            }
            catch (Exception ex)
            {
                await SendErrorAsync(ex.Message);
            }
        }
    }
}