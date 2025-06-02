using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace CompilerBrain;

public class SessionMemory
{
    ConcurrentDictionary<Guid, CompilerSession> sessions;

    public SessionMemory()
    {
        sessions = new ConcurrentDictionary<Guid, CompilerSession>();
    }

    public Guid CreateNewSession()
    {
        var id = Guid.NewGuid();
        var session = new CompilerSession(DateTime.UtcNow);
        sessions.TryAdd(id, session);
        return id;
    }

    public CompilerSession GetSession(Guid id)
    {
        if (!sessions.TryGetValue(id, out var session))
        {
            throw new InvalidOperationException("Session is not found. Id:" + id);
        }
        return session;
    }
}

public class CompilerSession(DateTime startTime)
{
    public DateTime StartTime { get; } = startTime;

    public Compilation? compilation;

    public Compilation GetCompilation()
    {
        if (compilation == null)
        {
            throw new InvalidOperationException("Compilation is not set.");
        }
        return compilation;
    }

    public void SetCompilation(Compilation compilation)
    {
        this.compilation = compilation;
    }
}

public class CodeDiagnostic
{
    public string Code { get; }
    public string Description { get; }
    public string FilePath { get; }
    public CodeLocation Location { get; }

    public CodeDiagnostic(Diagnostic diagnostic)
    {
        Code = diagnostic.Id;
        Description = diagnostic.ToString();
        FilePath = diagnostic.Location.SourceTree?.FilePath ?? "";
        Location = new(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);
    }

    public static CodeDiagnostic[] Errors(ImmutableArray<Diagnostic> diagnostics)
    {
        return diagnostics
            .Where(x => x.Severity == DiagnosticSeverity.Error)
            .Select(x => new CodeDiagnostic(x))
            .ToArray();
    }
}

public readonly record struct CodeLocation(int Start, int Length);



public class CodeStructure
{
    public required int Page { get; init; }
    public required int TotalPage { get; init; }
    public required Code[] Codes { get; init; }
}

public class Code
{
    public required string FilePath { get; init; }
    public required string CodeWithoutBody { get; init; }
}
