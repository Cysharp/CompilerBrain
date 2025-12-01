using CompilerBrain;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace CompilerBrain;

public class CompilerBrainChatService
{
    ChatClientAgent agent;
    AgentThread thread;

    public CompilerBrainChatService(ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IChatClient chatClient, CompilerBrainAIFunctions functions)
    {
        this.agent = chatClient.CreateAIAgent(
           instructions: "You are C# expert.",
           name: "Main Agent",
           description: "An AI agent that helps with C# programming tasks.",
           tools: functions.GetAIFunctions().ToArray(),
           loggerFactory: loggerFactory,
           services: serviceProvider);

        this.thread = agent.GetNewThread();
    }

    public async Task<AgentRunResponse> RunAsync(string message, CancellationToken cancellationToken)
    {
        return await agent.RunAsync(message, thread, cancellationToken: cancellationToken);
    }
}
