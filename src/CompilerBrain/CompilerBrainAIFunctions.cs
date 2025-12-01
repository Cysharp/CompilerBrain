using Microsoft.CodeAnalysis;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using ZLinq;

namespace CompilerBrain;

public class CompilerBrainAIFunctions(SessionMemory memory)
{
    public IEnumerable<AIFunction> GetAIFunctions()
    {
        yield return AIFunctionFactory.Create(GetProjects);
        yield return AIFunctionFactory.Create(GetDiagnostics);
        yield return AIFunctionFactory.Create(ReadCode);
    }

    [Description("Get project names of loaded solution.")]
    public string[] GetProjects()
    {
        return memory.Compilations.Select(x => x.Name).ToArray(); // TODO: directory path?
    }

    [Description("Get error diagnostics of the target project.")]
    public CodeDiagnostic[] GetDiagnostics([Description("Project Name.")] string projectName)
    {
        var diagnostics = GetCompilation(projectName).GetDiagnostics();
        return CodeDiagnostic.Errors(diagnostics);
    }

    [Description("Read existing code in current session context, if not found returns null.")]
    public string? ReadCode(string projectName, string filePath, string code)
    {
        // TODO: fullpath or candidate
        var compilation = GetCompilation(projectName);

        if (!compilation.SyntaxTrees.TryGet(filePath, out var existingTree) || !existingTree.TryGetText(out var text))
        {
            return null;
        }

        return text.ToString();
    }

    Compilation GetCompilation(string projectName)
    {
        var project = memory.Compilations.FirstOrDefault(x => x.Name == projectName);
        if (project.Compilation == null) throw new ArgumentException($"Project '{projectName}' not found in session context.");
        return project.Compilation;
    }

    //    [McpServerTool, Description("Add or replace new code to current session context, returns diagnostics of compile result.")]
    //    public static AddOrReplaceResult AddOrReplaceCode(SessionMemory memory, Guid sessionId, string projectFilePath, Codes[] codes)
    //    {
    //        try
    //        {
    //            var session = memory.GetSession(sessionId);
    //            var project = memory.GetSession(sessionId).Solution.GetProject(projectFilePath);
    //            var compilation = project.Compilation;
    //            var parseOptions = project.ParseOptions;

    //            if (codes.Length == 0)
    //            {
    //                return new AddOrReplaceResult { CodeChanges = [], Diagnostics = [] };
    //            }

    //            Compilation newCompilation = default!;
    //            List<CodeChange> codeChanges = new();
    //            foreach (var item in codes)
    //            {
    //                var code = item.Code;
    //                var filePath = item.FilePath;

    //                if (compilation.SyntaxTrees.TryGet(filePath, out var oldTree))
    //                {
    //                    var lineBreak = oldTree.GetLineBreakFromFirstLine();
    //                    code = code.ReplaceLineEndings(lineBreak);

    //                    var newTree = oldTree.WithChangedText(SourceText.From(code));
    //                    var changes = newTree.GetChanges(oldTree);

    //                    var lineChanges = new LineChanges[changes.Count];
    //                    var i = 0;
    //                    foreach (var change in changes)
    //                    {
    //                        var changeText = GetLineText(oldTree, change.Span);
    //                        lineChanges[i++] = new LineChanges { RemoveLine = changeText.ToString(), AddLine = change.NewText };
    //                    }

    //                    codeChanges.Add(new CodeChange { FilePath = filePath, LineChanges = lineChanges });
    //                    newCompilation = compilation.ReplaceSyntaxTree(oldTree, newTree);
    //                    project.RemoveNewCode(oldTree);
    //                    project.AddNewCode(newTree);
    //                }
    //                else
    //                {
    //                    var syntaxTree = CSharpSyntaxTree.ParseText(code, options: parseOptions, path: filePath);
    //                    codeChanges.Add(new CodeChange { FilePath = filePath, LineChanges = [new LineChanges { RemoveLine = null, AddLine = code }] });
    //                    newCompilation = compilation.AddSyntaxTrees(syntaxTree);
    //                    project.AddNewCode(syntaxTree);
    //                }
    //            }

    //            project.Compilation = newCompilation;
    //            var diagnostics = CodeDiagnostic.Errors(newCompilation.GetDiagnostics());

    //            var result = new AddOrReplaceResult
    //            {
    //                CodeChanges = codeChanges.ToArray(),
    //                Diagnostics = diagnostics
    //            };

    //            return result;
    //        }
    //        catch (Exception ex)
    //        {
    //            throw new McpException(ex.Message, ex);
    //        }
    //    }
}
