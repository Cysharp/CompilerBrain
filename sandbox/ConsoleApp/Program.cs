//using CompilerBrain;
//using System.Text.Json;

//var memory = new SessionMemory();

//var id = CSharpMcpServer.Initialize(memory);

////var path = @"C"";
//// path = @"C:\ZLinq\ZLinq.slnx";
////var hugahuga = await CSharpMcpServer.OpenCSharpSolution(memory, id, path);


////var diagnostics = await CSharpMcpServer.OpenCsharpProject(memory, id, @"C:\ZLinq\src\ZLinq\ZLinq.csproj");
//var diagnostics = await CSharpMcpServer.OpenCSharpProject(memory, id, @"C:\MyGit\ZLinq\src\ZLinq\ZLinq.csproj");

//// C:\ZLinq\ZLinq.slnx



////var list = new List<CodeStructure>();
////var page = 0;
////CodeStructure codeStructure = default!;
////do
////{
////    page++;
////    Console.WriteLine("Read Page:" + page);
////    var structure = CSharpMcpServer.GetCodeStructure(memory, id, page);
////    list.Add(structure);
////    codeStructure = structure;
////} while (codeStructure.TotalPage != page);

////foreach (var item in list)
////{
////    foreach (var item2 in item.Codes)
////    {
////        Console.WriteLine(item2.CodeWithoutBody);
////    }
////}

////Console.WriteLine("foo");


////var result = CSharpMcpServer.AddOrReplaceCode(memory, id, new[] {
////    new Codes
////    {
////        // FilePath =  @"C:\ZLinq\src\ZLinq\Test.cs",
////        FilePath =  @"C:\MyGit\ZLinq\src\ZLinq\Test.cs",
////        Code = """
////namespace ZLinq;

////public class Test
////{
////}
////"""
////    }
////});

