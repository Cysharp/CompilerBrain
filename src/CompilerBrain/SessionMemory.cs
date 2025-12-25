using CompilerBrain;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using ZLinq;
using ZLogger;

namespace CompilerBrain;

public class SessionMemory(ILogger<SessionMemory> logger, SolutionLoadProgress progress)
{
    bool initialized = false;

    Solution solution = default!;
    (Project Project, Compilation Compilation)[] projects = default!;

    (string ProjectName, Compilation Compilation, string[] ChangedFilePaths) lastChanged = default!;

    public Solution Solution => solution;
    public ReadOnlyMemory<(Project Project, Compilation Compilation)> Projects => projects;

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

        (this.solution, this.projects) = await OpenCSharpSolutionAsync(filePath, cancellationToken);
        this.initialized = true;

        logger.ZLogInformation($"Initialize Complete");
        return true;
    }

    public (Project Project, Compilation Compilation) GetProjectAndCompilation(string projectName)
    {
        var project = Projects.FirstOrDefault(x => x.Project.Name == projectName);
        if (project.Project == null) throw new ArgumentException($"Project '{projectName}' not found in session context.");
        return project;
    }

    public Project GetProject(string projectName) => GetProjectAndCompilation(projectName).Project;

    public Compilation GetCompilation(string projectName) => GetProjectAndCompilation(projectName).Compilation;

    async Task<(Solution, (Project, Compilation)[])> OpenCSharpSolutionAsync(string solutionPath, CancellationToken cancellationToken)
    {
        using var workspace = MSBuildWorkspace.Create();

        var solution = await workspace.OpenSolutionAsync(solutionPath, progress, cancellationToken: cancellationToken);

        var compilations = new List<(Project, Compilation)>();
        foreach (var item in solution.Projects)
        {
            logger.ZLogInformation($"Opening Project Copmilation: {item.Name}");

            var compilation = await item.GetCompilationAsync(cancellationToken);
            if (compilation != null)
            {
                compilations.Add((item, compilation));
            }
        }

        return (solution, compilations.ToArray());
    }

    public void SetChangedCodes(string projectName, Compilation compilation, string[] filePaths)
    {
        lastChanged = (projectName, compilation, filePaths);
    }

    public (Compilation Compilation, string[] ChangedFilePaths)? FlushChangedCodes()
    {
        var project = projects.Index().FirstOrDefault(x => x.Item.Project.Name == lastChanged.ProjectName);
        if (project.Item.Project == null) return null;

        projects[project.Index] = (project.Item.Project, lastChanged.Compilation);

        var result = (lastChanged.Compilation, lastChanged.ChangedFilePaths);
        lastChanged = default;
        return result;
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
