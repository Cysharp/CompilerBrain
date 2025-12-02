using CompilerBrain;
using Microsoft.Agents.AI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ZLinq;

namespace CompilerBrain;

public class CompilerBrainChatService
{
    ChatClientAgent agent;
    AgentThread thread;

    public CompilerBrainChatService(ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IChatClient chatClient, CompilerBrainAIFunctions functions, SessionMemory memory)
    {
        this.agent = chatClient.CreateAIAgent(
           instructions: CreateInstructions(memory),
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


#pragma warning disable MEAI001
    public async Task<AgentRunResponse> ContinueAsync(ResponseContinuationToken continuationToken, CancellationToken cancellationToken)
#pragma warning restore MEAI001
    {
        return await agent.RunAsync(thread, new AgentRunOptions { ContinuationToken = continuationToken }, cancellationToken);
    }

    string CreateInstructions(SessionMemory memory)
    {
        var runtimeInfo = MachineRuntimeInformation.FromCurrent();

        var inst =
$$"""
You are "Compiler Brain", an interactive CLI agent. As an agent specialized in C# coding, leverage your tools to assist with coding tasks. All tasks are performed on the currently opened solution, and all operations are executed against the in-memory Compilation.

# Environment

The user's environment is as follows:

- **dotnet version** - {{runtimeInfo.FrameworkDescription}}
- **OperatingSystem** - {{runtimeInfo.OperatingSystemPlatform}}

# Workflow

- If project details are not provided, check `ReadMe.md` and `.csproj` dependencies to understand each project's role
- Base coding style (indentation, naming conventions, etc.) on existing structures within the project
- Once an absolute path is obtained, use that absolute path for subsequent tool calls
- When searching with regular expressions, remove `\bin\` and `\obj\` from the search path.
- If the `AddOrReplaceCode` tool call fails to compile, analyze the cause and retry with different code up to 3 times. On success, only one version is retained in the in-memory Compilation. On failure, nothing is retained
- Code edited with `AddOrReplaceCode` has not yet been written to disk; always ask the user for confirmation before calling `SaveChangedCodeToDisc`

# Output Guideline

- **Use plain text** Output as readable plain text rather than markdown, since this is a command-line tool
- **Be concise** The target audience is C# professionals; verbose explanations are unnecessary, though prioritize clarity when requested by the user or deemed important
- **Avoid unnecessary output** No pleasantries or greetings
""";

        return inst;
    }
}
