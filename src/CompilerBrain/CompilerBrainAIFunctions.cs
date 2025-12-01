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
    }

    [Description("Get project names of loaded solution.")]
    public string[] GetProjects()
    {
        return memory.Compilations.Select(x => x.Name).ToArray(); // TODO: directory path?
    }

    [Description("Get error diagnostics of the target project.")]
    public CodeDiagnostic[] GetDiagnostics([Description("Project Name.")] string projectName)
    {
        var project = memory.Compilations.FirstOrDefault(x => x.Name == projectName);
        if (project.Compilation == null) throw new ArgumentException($"Project '{projectName}' not found in session context.");

        var diagnostics = project.Compilation.GetDiagnostics();
        return CodeDiagnostic.Errors(diagnostics);
    }
}
