using CompilerBrain;
using System.Text.Json;

var memory = new SessionMemory();

var id = CSharpMcpServer.Initialize(memory);


var diagnostics = await CSharpMcpServer.OpenCsharpProject(memory, id, @"C:\ZLinq\src\ZLinq\ZLinq.csproj");


var list = new List<CodeStructure>();
var page = 0;
CodeStructure codeStructure = default!;
do
{
    page++;
    Console.WriteLine("Read Page:" + page);
    var structure = CSharpMcpServer.GetCodeStructure(memory, id, page);
    list.Add(structure);
    codeStructure = structure;
} while (codeStructure.TotalPage != page);

foreach (var item in list.SelectMany(x => x.Codes))
{
    Console.WriteLine(item.FilePath);
}


