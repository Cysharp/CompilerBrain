using System.Text.Json;
using Xunit.v3;

namespace CompilerBrain.Tests;

public class UnitTest1(ITestContextAccessor testContextAccessor)
{
    [Fact]
    public async Task Test1()
    {
        var memory = new SessionMemory();

        var id = CSharpMcpServer.Initialize(memory);

        // open self

        var currentTest = testContextAccessor.Current;
        var asmPath = currentTest.TestAssembly!.AssemblyPath;
        // var path = Path.Combine(asmPath, "../../", "../../CompilerBrain.Tests.csproj");
        var path = Path.Combine(asmPath, "../../../../../../sandbox/ConsoleApp/ConsoleApp.csproj");



        var diagnostics = await CSharpMcpServer.OpenCSharpProject(memory, id, path);


        var json = JsonSerializer.Serialize(diagnostics);
    }
}
