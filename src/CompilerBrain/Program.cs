using Anthropic;
using CompilerBrain;
using ConsoleAppFramework;
using Microsoft.Agents.AI;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;

var app = ConsoleApp.Create();

app.ConfigureDefaultConfiguration(builder =>
{
    builder.AddUserSecrets<Program>();
});

app.ConfigureServices((configuration, services) =>
{
    // memory is mutable, process-wide scope.
    services.AddSingleton<SessionMemory>();
    services.AddTransient<SolutionLoadProgress>();
    services.AddSingleton<CompilerBrainAIFunctions>();

    // make chat-client
    var key = configuration.GetSection("ANTHROPIC_API_KEY").Value ?? null;
    var model = "claude-haiku-4-5-20251001";

    services.AddSingleton<IChatClient>(serviceProvider =>
    {
        var builder = new AnthropicClient(new Anthropic.Core.ClientOptions { APIKey = key })
            .AsIChatClient(model)
            .AsBuilder();
        builder.UseFunctionInvocation(serviceProvider.GetRequiredService<ILoggerFactory>());
        return builder.Build(serviceProvider);
    });

    services.AddSingleton<CompilerBrainChatService>();
});

app.ConfigureLogging(x =>
{
    x.ClearProviders();
    x.SetMinimumLevel(LogLevel.Trace);
    x.AddZLoggerConsole();
});

app.Add<Commands>();

// call initialize
await app.RunAsync(args, disposeServiceProvider: false);

// If ConsoleAppFramework throws error/canceled, ExitCode will be set to non-zero.
while (Environment.ExitCode == 0)
{
    var command = Console.ReadLine();
    if (command == null) break;

    await app.RunAsync([command], disposeServiceProvider: false);
}

await app.RunAsync([], disposeServiceProvider: true); // Dispose at the end
