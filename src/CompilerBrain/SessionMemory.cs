using CompilerBrain;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace CompilerBrain;

public class SessionMemory(ILogger<SessionMemory> logger, SolutionLoadProgress progress)
{
    bool initialized = false;

    Solution solution = default!;
    (string Name, Compilation Compilation)[] compilations = default!;

    public Solution Solution => solution;
    public ReadOnlyMemory<(string Name, Compilation Compilation)> Compilations => compilations;

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

        (this.solution, this.compilations) = await OpenCSharpSolutionAsync(filePath, cancellationToken);
        this.initialized = true;

        logger.ZLogInformation($"Initialize Complete");
        return true;
    }

    async Task<(Solution, (string, Compilation)[])> OpenCSharpSolutionAsync(string solutionPath, CancellationToken cancellationToken)
    {
        using var workspace = MSBuildWorkspace.Create();

        var solution = await workspace.OpenSolutionAsync(solutionPath, progress, cancellationToken: cancellationToken);

        var compilations = new List<(string, Compilation)>();
        foreach (var item in solution.Projects)
        {
            logger.ZLogInformation($"Opening Project Copmilation: {item.Name}");

            var compilation = await item.GetCompilationAsync(cancellationToken);
            if (compilation != null)
            {

                compilations.Add((item.Name, compilation));
            }
        }

        return (solution, compilations.ToArray());
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
