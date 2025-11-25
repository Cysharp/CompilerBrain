//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using System.Collections.Concurrent;
//using System.Collections.Immutable;
//using System.Diagnostics.CodeAnalysis;

//namespace CompilerBrain;

//public class SessionMemory
//{
//    ConcurrentDictionary<Guid, CompilerSession> sessions;

//    public SessionMemory()
//    {
//        sessions = new ConcurrentDictionary<Guid, CompilerSession>();
//    }

//    public Guid CreateNewSession()
//    {
//        var id = Guid.NewGuid();
//        var session = new CompilerSession(DateTime.UtcNow);
//        sessions.TryAdd(id, session);
//        return id;
//    }

//    public CompilerSession GetSession(Guid id)
//    {
//        if (!sessions.TryGetValue(id, out var session))
//        {
//            throw new InvalidOperationException("Session is not found. Id:" + id);
//        }
//        return session;
//    }

//    // shortcut
//    public Compilation GetCompilation(Guid sessionId, string projectFilePath)
//    {
//        var session = this.GetSession(sessionId);
//        var compilation = session.Solution.GetProject(projectFilePath).Compilation;
//        return compilation;
//    }
//}

//public class CompilerSession(DateTime startTime)
//{
//    CSharpSolution? solution;

//    public DateTime StartTime { get; } = startTime;

//    public bool HasSolution => solution != null;

//    public CSharpSolution Solution
//    {
//        get
//        {
//            if (solution == null)
//            {
//                throw new InvalidOperationException("Solution is not set.");
//            }
//            return solution;
//        }
//        set
//        {
//            solution = value;
//        }
//    }
//}


//public class CSharpSolution(Solution solution)
//{
//    Dictionary<string, CSharpProject> projects = new();

//    public Solution RawSolution => solution;

//    public bool TryAddProject(Project project)
//    {
//        if (project.Language != LanguageNames.CSharp || project.FilePath == null || !project.SupportsCompilation)
//        {
//            return false;
//        }

//        // key is filePath
//        projects.Add(project.FilePath, new CSharpProject(project));
//        return true;
//    }

//    public bool TryGetProject(string projectFilePath, [MaybeNullWhen(false)] out CSharpProject project)
//    {
//        return projects.TryGetValue(projectFilePath, out project);
//    }

//    public CSharpProject GetProject(string projectFilePath)
//    {
//        if (!projects.TryGetValue(projectFilePath, out var project))
//        {
//            throw new InvalidOperationException("Project is not found in this solution.");
//        }
//        return project;
//    }

//    public IEnumerable<CSharpProject> Projects => projects.Values;
//}

//public class CSharpProject
//{
//    readonly Project project;
//    readonly CSharpParseOptions parseOptions;

//    // Compilation is lazy open.
//    Compilation? compilation;
//    HashSet<SyntaxTree>? newCodes;

//    public CSharpProject(Project project)
//    {
//        this.project = project;
//        this.parseOptions = (CSharpParseOptions)project.ParseOptions!;
//    }

//    public CSharpParseOptions ParseOptions => parseOptions;

//    public Compilation Compilation
//    {
//        get
//        {
//            if (compilation == null)
//            {
//                throw new InvalidOperationException("Compilation is not set.");
//            }
//            return compilation;
//        }
//        set
//        {
//            compilation = value;
//        }
//    }

//    public Task<Compilation> GetNewProjectCompilationAsync()
//    {
//        return project.GetCompilationAsync()!;
//    }

//    // track new-code for save to disc.

//    public void AddNewCode(SyntaxTree syntaxTree)
//    {
//        if (newCodes == null) newCodes = new();
//        newCodes.Add(syntaxTree);
//    }

//    public void RemoveNewCode(SyntaxTree syntaxTree)
//    {
//        if (newCodes == null) newCodes = new();
//        newCodes.Remove(syntaxTree);
//    }

//    public SyntaxTree[] GetNewCodesAndClear()
//    {
//        if (newCodes == null) return [];

//        var result = newCodes.ToArray();
//        newCodes.Clear();
//        return result;
//    }
//}


//public class CodeDiagnostic
//{
//    public string Code { get; }
//    public string Description { get; }
//    public string FilePath { get; }
//    public CodeLocation Location { get; }

//    public CodeDiagnostic(Diagnostic diagnostic)
//    {
//        Code = diagnostic.Id;
//        Description = diagnostic.ToString();
//        FilePath = diagnostic.Location.SourceTree?.FilePath ?? "";
//        Location = new(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);
//    }

//    public static CodeDiagnostic[] Errors(ImmutableArray<Diagnostic> diagnostics)
//    {
//        return diagnostics
//            .Where(x => x.Severity == DiagnosticSeverity.Error)
//            .Select(x => new CodeDiagnostic(x))
//            .ToArray();
//    }
//}

