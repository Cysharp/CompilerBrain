//using CompilerBrain;
//using Microsoft.Build.Locator;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
using CompilerBrain;
using ConsoleAppFramework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLinq;
//using ModelContextProtocol.Protocol;
//using System.Text.Encodings.Web;
//using System.Text.Json;
//using System.Threading;
using ZLogger;

// ZLinq drop-in everything
[assembly: ZLinqDropInAttribute("", ZLinq.DropInGenerateTypes.Everything)]
[assembly: ZLinqDropInExternalExtension("", "System.Collections.Immutable.ImmutableArray`1", "ZLinq.Linq.FromImmutableArray`1")]

//// System.Diagnostics.Debugger.Launch(); // for DEBUGGING.

//MSBuildLocator.RegisterDefaults();

//var jsonSerializerOptions = new JsonSerializerOptions
//{
//    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
//    WriteIndented = false,
//    TypeInfoResolver = JsonSerializerOptions.Default.TypeInfoResolver
//};

//var builder = Host.CreateApplicationBuilder(args);
//builder.Logging.ClearProviders();
//builder.Logging.SetMinimumLevel(LogLevel.Trace);
//builder.Logging.AddZLoggerConsole(consoleLogOptions =>
//{
//    // Configure all logs to go to stderr
//    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
//});

//builder.Services
//    .AddSingleton<SessionMemory>()
//    .AddMcpServer(serverOptions =>
//    {
//    })
//    .WithStdioServerTransport()
//    .WithTools([typeof(CSharpMcpServer)], jsonSerializerOptions);

//await builder.Build().RunAsync();

// Environment.CurrentDirectory = "C:\\Users\\\Documents\\GitHub\\ConsoleAppFramework";


var app = ConsoleApp.Create();


app.ConfigureServices(services =>
{
    // memory is mutable, process-wide scope.
    services.AddSingleton<SessionMemory>();
    services.AddTransient<SolutionLoadProgress>();
});

app.ConfigureLogging(x =>
{
    x.ClearProviders();
    x.SetMinimumLevel(LogLevel.Trace);
    x.AddZLoggerConsole();
});

app.Add<Commands>();

await app.RunAsync(args);


//var fsw = new FileSystemWatcher("", ".cs|.csproj|.sln");
//var result = fsw.WaitForChanged(WatcherChangeTypes.All);


public class Commands(SessionMemory memory, ILogger<Commands> logger)
{
    [Command("")]
    public async Task RootCommand([Argument] string? command = null, CancellationToken cancellationToken = default)
    {
        if (await memory.TryInitializeAsync(command, cancellationToken))
        {
            // logger.ZLogInformation($"Session memory initialized. WorkingDirectory={memory.WorkingDirectory}");
        }
        // command handling
    }
}

public class SessionMemory(ILogger<SessionMemory> logger, SolutionLoadProgress progress)
{
    bool initialized = false;


    //string? workingDirectory;
    //public string? WorkingDirectory => workingDirectory;

    public async ValueTask<bool> TryInitializeAsync(string? filePath, CancellationToken cancellationToken)
    {
        if (initialized) return false;

        if (filePath == null)
        {
            var currentDirectory = Environment.CurrentDirectory;

            var slnx = Directory.GetFiles(currentDirectory, "*.slnx"); // TODO: support .sln also?
            if (slnx.Length == 0)
            {
                throw new Exception("Solution file is not found in the current directory: " + currentDirectory);
            }
            if (slnx.Length > 1)
            {
                throw new Exception("Multiple solution files are found in the current directory: " + currentDirectory);
            }

            filePath = slnx[0];
        }

        logger.ZLogInformation($"Opening Solution: {filePath}");

        await OpenCSharpSolutionAsync(filePath, cancellationToken);

        logger.ZLogInformation($"Initialize Complete");


        // workingDirectory = directory;
        initialized = true;
        return true;
    }

    async Task OpenCSharpSolutionAsync(string solutionPath, CancellationToken cancellationToken)
    {
        using var workspace = MSBuildWorkspace.Create();

        var solution = await workspace.OpenSolutionAsync(solutionPath, progress, cancellationToken: cancellationToken);

        var compilations = new List<Compilation>();
        foreach (var item in solution.Projects)
        {
            logger.ZLogInformation($"Opening Project Copmilation: {item.Name}");

            var compilation = await item.GetCompilationAsync(cancellationToken);
            if (compilation != null)
            {
                compilations.Add(compilation);
            }
        }
    }
}

public class SolutionLoadProgress(ILogger<SolutionLoadProgress> logger) : IProgress<ProjectLoadProgress>
{
    public void Report(ProjectLoadProgress value)
    {
        var projectName = Path.GetFileNameWithoutExtension(value.FilePath);
        var elapsed = value.ElapsedTime.TotalSeconds.ToString("F2") + "sec";
        logger.ZLogInformation($"    {value.Operation}: {new[] { projectName, value.TargetFramework, elapsed }.Join(" - ")}");
    }
}
