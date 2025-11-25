//using CompilerBrain;
//using Microsoft.Build.Locator;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using ModelContextProtocol.Protocol;
//using System.Text.Encodings.Web;
//using System.Text.Json;
//using System.Threading;
//using ZLogger;

//// ZLinq drop-in everything
//[assembly: ZLinqDropInAttribute("", ZLinq.DropInGenerateTypes.Everything)]
//[assembly: ZLinqDropInExternalExtension("", "System.Collections.Immutable.ImmutableArray`1", "ZLinq.Linq.FromImmutableArray`1")]

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


Console.WriteLine("reuse");
