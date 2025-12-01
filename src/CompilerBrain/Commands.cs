using ConsoleAppFramework;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace CompilerBrain;

public class Commands(SessionMemory memory, CompilerBrainChatService chatService, ILogger<Commands> logger)
{
    [Command("")]
    public async Task RootCommand([Argument] string? command = null, CancellationToken cancellationToken = default)
    {
        if (await memory.TryInitializeAsync(command, cancellationToken))
        {
            logger.ZLogInformation($"Session memory initialized.");
            return;
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            logger.ZLogTrace($"Receive Empty Command.");
            return;
        }

        var response = await chatService.RunAsync(command, cancellationToken);
        logger.ZLogInformation($"{response.AsChatResponse().Text}");
    }

    // public readonly record struct ProjectNameAndFilePath(string ProjectName, string ProjectFilePath);


}
