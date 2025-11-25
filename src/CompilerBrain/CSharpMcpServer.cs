//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.MSBuild;
//using Microsoft.CodeAnalysis.Text;
//using ModelContextProtocol;
//using ModelContextProtocol.Server;
//using System.ComponentModel;
//using System.Text;
//using System.Text.RegularExpressions;

//namespace CompilerBrain;

//[McpServerToolType]
//public static partial class CSharpMcpServer
//{
//    static Encoding Utf8Encoding = new UTF8Encoding(false);

//    [McpServerTool, Description("Initialize the session context, require to call first before call other tools.")]
//    public static Guid Initialize(SessionMemory memory)
//    {
//        return memory.CreateNewSession();
//    }

//    [McpServerTool, Description("Open solution file (.sln/.slnx) of the session context, returns project name and file-paths.")]
//    public static async Task<ProjectNameAndFilePath[]> OpenCSharpSolution(SessionMemory memory, Guid sessionId, string solutionPath)
//    {
//        using var workspace = MSBuildWorkspace.Create();
//        var solution = await workspace.OpenSolutionAsync(solutionPath);

//        var session = memory.GetSession(sessionId);

//        var sln = new CSharpSolution(solution);
//        var list = new List<ProjectNameAndFilePath>();
//        foreach (var item in solution.Projects)
//        {
//            if (sln.TryAddProject(item))
//            {
//                list.Add(new ProjectNameAndFilePath(item.Name, item.FilePath!));
//            }
//        }

//        session.Solution = sln;
//        return list.ToArray();
//    }

//    [McpServerTool, Description("Open csproject of the session context, returns diagnostics of compile result.")]
//    public static async Task<CodeDiagnostic[]> OpenCSharpProject(SessionMemory memory, Guid sessionId, string projectFilePath)
//    {
//        var session = memory.GetSession(sessionId);

//        if (!session.HasSolution)
//        {
//            // create project only solution
//            using var workspace = MSBuildWorkspace.Create();
//            var proj = await workspace.OpenProjectAsync(projectFilePath);

//            var sln = new CSharpSolution(proj.Solution);
//            if (!sln.TryAddProject(proj))
//            {
//                throw new InvalidOperationException("Can't open compilable project.");
//            }

//            session.Solution = sln;
//        }

//        if (!session.Solution.TryGetProject(projectFilePath, out var project))
//        {
//            throw new InvalidOperationException("Can't find project in solution.");
//        }

//        var compilation = await project.GetNewProjectCompilationAsync();
//        if (compilation == null)
//        {
//            throw new InvalidOperationException("Can't get compilation.");
//        }

//        project.Compilation = compilation;
//        return CodeDiagnostic.Errors(compilation.GetDiagnostics());
//    }

//    [McpServerTool, Description("Get filepath and code without method-body to analyze csprojct. Data is paging so need to read mulitiple times. start page is one.")]
//    public static CodeStructure GetCodeStructure(SessionMemory memory, Guid sessionId, string projectFilePath, int page)
//    {
//        const int FilesPerPage = 30;

//        var compilation = memory.GetCompilation(sessionId, projectFilePath);

//        var trees = compilation.SyntaxTrees
//            .Where(x => File.Exists(x.FilePath))
//            .ToArray();

//        var totalPage = trees.Length / FilesPerPage + 1;

//        var codes = trees.Skip((page - 1) * FilesPerPage)
//            .Take(FilesPerPage)
//            .Select(x => new AnalyzedCode
//            {
//                FilePath = x.FilePath,
//                CodeWithoutBody = CodeCompression.RemoveBody(x)
//            })
//            .ToArray();

//        return new CodeStructure
//        {
//            Page = page,
//            TotalPage = totalPage,
//            Codes = codes
//        };
//    }

//    [McpServerTool, Description("Read existing code in current session context, if not found returns null.")]
//    public static string? ReadCode(SessionMemory memory, Guid sessionId, string projectFilePath, string filePath, string code)
//    {
//        var compilation = memory.GetCompilation(sessionId, projectFilePath);

//        if (!compilation.SyntaxTrees.TryGet(filePath, out var existingTree) || !existingTree.TryGetText(out var text))
//        {
//            return null;
//        }

//        return text.ToString();
//    }

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

//    [McpServerTool, Description("Insert code at specified position in an existing file. Supports various insertion modes like at position, at line start/end, before/after line.")]
//    public static InsertCodeResult InsertCode(SessionMemory memory, Guid sessionId, string projectFilePath, InsertCodeRequest[] insertRequests)
//    {
//        try
//        {
//            var session = memory.GetSession(sessionId);
//            var project = memory.GetSession(sessionId).Solution.GetProject(projectFilePath);
//            var compilation = project.Compilation;
//            var parseOptions = project.ParseOptions;

//            if (insertRequests.Length == 0)
//            {
//                return new InsertCodeResult { CodeChanges = [], Diagnostics = [] };
//            }

//            Compilation newCompilation = compilation;
//            List<CodeChange> codeChanges = new();
//            foreach (var request in insertRequests)
//            {
//                if (!newCompilation.SyntaxTrees.TryGet(request.FilePath, out var oldTree))
//                {
//                    throw new InvalidOperationException($"File not found in compilation: {request.FilePath}");
//                }

//                if (!oldTree.TryGetText(out var sourceText))
//                {
//                    throw new InvalidOperationException($"Cannot get text from file: {request.FilePath}");
//                }

//                var insertPosition = CalculateInsertPosition(sourceText, request.Position, request.Mode);
//                var lineBreak = oldTree.GetLineBreakFromFirstLine();
//                var codeToInsert = request.CodeToInsert.ReplaceLineEndings(lineBreak);

//                // Add line breaks if needed based on insert mode
//                if (request.Mode == InsertMode.AfterLine || request.Mode == InsertMode.BeforeLine)
//                {
//                    if (!codeToInsert.EndsWith(lineBreak))
//                    {
//                        codeToInsert += lineBreak;
//                    }
//                }

//                var newText = sourceText.WithChanges(new TextChange(new TextSpan(insertPosition, 0), codeToInsert));
//                var newTree = oldTree.WithChangedText(newText);

//                // Calculate changes for reporting
//                var changes = newTree.GetChanges(oldTree);
//                var lineChanges = new List<LineChanges>();

//                foreach (var change in changes)
//                {
//                    if (change.Span.Length == 0) // Insert operation
//                    {
//                        lineChanges.Add(new LineChanges { RemoveLine = null, AddLine = change.NewText });
//                    }
//                    else // Replace operation (shouldn't happen in pure insert, but safety)
//                    {
//                        var changeText = GetLineText(oldTree, change.Span);
//                        lineChanges.Add(new LineChanges { RemoveLine = changeText.ToString(), AddLine = change.NewText });
//                    }
//                }

//                codeChanges.Add(new CodeChange { FilePath = request.FilePath, LineChanges = lineChanges.ToArray() });
//                newCompilation = newCompilation.ReplaceSyntaxTree(oldTree, newTree);
//                project.RemoveNewCode(oldTree);
//                project.AddNewCode(newTree);
//            }

//            project.Compilation = newCompilation;
//            var diagnostics = CodeDiagnostic.Errors(newCompilation.GetDiagnostics());

//            return new InsertCodeResult
//            {
//                CodeChanges = codeChanges.ToArray(),
//                Diagnostics = diagnostics
//            };
//        }
//        catch (Exception ex)
//        {
//            throw new McpException(ex.Message, ex);
//        }
//    }

//    [McpServerTool, Description("Save all add/modified codes in current in-memory session context, return value is saved paths.")]
//    public static string[] SaveCodeToDisc(SessionMemory memory, Guid sessionId)
//    {
//        var session = memory.GetSession(sessionId);

//        var result = new List<string>();

//        foreach (var proj in session.Solution.Projects)
//        {
//            var newSources = proj.GetNewCodesAndClear();
//            foreach (var item in newSources)
//            {
//                File.WriteAllText(item.FilePath, item.GetText().ToString(), Utf8Encoding);
//                result.Add(item.FilePath);
//            }
//        }

//        return result.ToArray();
//    }

//    [McpServerTool, Description("Search for code patterns using regular expressions in files matching the target file pattern.")]
//    public static SearchResult SearchCodeByRegex(SessionMemory memory, Guid sessionId, string projectFilePath, string targetFileRegex, string searchRegex)
//    {
//        var compilation = memory.GetCompilation(sessionId, projectFilePath);

//        // accept user generated regex patterns so no-compiled and non-backtracking options are used.
//        var targetFilePattern = new Regex(targetFileRegex, RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);
//        var searchPattern = new Regex(searchRegex, RegexOptions.Multiline | RegexOptions.NonBacktracking);

//        var matches = new List<SearchMatch>();

//        var targetTrees = compilation.SyntaxTrees
//            .Where(tree => !string.IsNullOrEmpty(tree.FilePath) &&
//                          File.Exists(tree.FilePath) &&
//                          targetFilePattern.IsMatch(Path.GetFileName(tree.FilePath)));

//        foreach (var syntaxTree in targetTrees)
//        {
//            if (syntaxTree.TryGetText(out var sourceText))
//            {
//                var fullText = sourceText.ToString();
//                var regexMatches = searchPattern.Matches(fullText);
//                var root = syntaxTree.GetRoot();

//                foreach (Match match in regexMatches)
//                {
//                    var textSpan = new TextSpan(match.Index, match.Length);
//                    var linePosition = sourceText.Lines.GetLinePosition(match.Index);
//                    var lineText = sourceText.Lines[linePosition.Line].ToString();

//                    var context = AnalyzeSyntaxContext(root, match.Index);

//                    matches.Add(new SearchMatch
//                    {
//                        FilePath = syntaxTree.FilePath,
//                        LineNumber = linePosition.Line + 1,
//                        ColumnNumber = linePosition.Character + 1,
//                        LineText = lineText,
//                        MatchedText = match.Value,
//                        Location = new CodeLocation(match.Index, match.Length),
//                        Context = context
//                    });
//                }
//            }
//        }

//        return new SearchResult
//        {
//            Matches = matches.ToArray(),
//            TotalMatches = matches.Count
//        };
//    }

//    // TODO: MPC API
//    static void RunUnitTest(SessionMemory memory, Guid sessionId, string projectFilePath)
//    {
//        var compilation = memory.GetCompilation(sessionId, projectFilePath);

//        using var libraryStream = new MemoryStream();
//        // var r = session.Compilation.Emit(libraryStream);
//    }

//    static int CalculateInsertPosition(SourceText sourceText, int position, InsertMode mode)
//    {
//        return mode switch
//        {
//            InsertMode.AtPosition => Math.Max(0, Math.Min(position, sourceText.Length)),
//            InsertMode.AtLineStart => CalculateLinePosition(sourceText, position, atStart: true),
//            InsertMode.AtLineEnd => CalculateLinePosition(sourceText, position, atStart: false),
//            InsertMode.BeforeLine => CalculateBeforeLinePosition(sourceText, position),
//            InsertMode.AfterLine => CalculateAfterLinePosition(sourceText, position),
//            _ => position
//        };
//    }

//    static int CalculateLinePosition(SourceText sourceText, int lineNumber, bool atStart)
//    {
//        var lines = sourceText.Lines;
//        var adjustedLineNumber = Math.Max(0, Math.Min(lineNumber - 1, lines.Count - 1));
//        var line = lines[adjustedLineNumber];
//        return atStart ? line.Start : line.End;
//    }

//    static int CalculateBeforeLinePosition(SourceText sourceText, int lineNumber)
//    {
//        var lines = sourceText.Lines;
//        var adjustedLineNumber = Math.Max(0, Math.Min(lineNumber - 1, lines.Count - 1));
//        return lines[adjustedLineNumber].Start;
//    }

//    static int CalculateAfterLinePosition(SourceText sourceText, int lineNumber)
//    {
//        var lines = sourceText.Lines;
//        var adjustedLineNumber = Math.Max(0, Math.Min(lineNumber - 1, lines.Count - 1));
//        return lines[adjustedLineNumber].End;
//    }

//    static CodeContext AnalyzeSyntaxContext(SyntaxNode root, int position)
//    {
//        var node = root.FindToken(position).Parent;

//        string? className = null;
//        string? methodName = null;
//        string? propertyName = null;
//        string? fieldName = null;
//        string? namespaceName = null;
//        string syntaxKind = "Unknown";
//        string containingMember = "Global";

//        var current = node;
//        while (current != null)
//        {
//            switch (current)
//            {
//                case NamespaceDeclarationSyntax ns:
//                    namespaceName = ns.Name.ToString();
//                    break;
//                case FileScopedNamespaceDeclarationSyntax fileNs:
//                    namespaceName = fileNs.Name.ToString();
//                    break;
//                case ClassDeclarationSyntax cls:
//                    className = cls.Identifier.ValueText;
//                    break;
//                case RecordDeclarationSyntax record:
//                    className = record.Identifier.ValueText + " (record)";
//                    break;
//                case StructDeclarationSyntax str:
//                    className = str.Identifier.ValueText + " (struct)";
//                    break;
//                case InterfaceDeclarationSyntax iface:
//                    className = iface.Identifier.ValueText + " (interface)";
//                    break;
//                case MethodDeclarationSyntax method:
//                    methodName = method.Identifier.ValueText;
//                    break;
//                case ConstructorDeclarationSyntax ctor:
//                    methodName = ".ctor";
//                    break;
//                case PropertyDeclarationSyntax prop:
//                    propertyName = prop.Identifier.ValueText;
//                    break;
//                case FieldDeclarationSyntax field when field.Declaration.Variables.Count > 0:
//                    fieldName = field.Declaration.Variables[0].Identifier.ValueText;
//                    break;
//                case LocalFunctionStatementSyntax localFunc:
//                    methodName = localFunc.Identifier.ValueText + " (local)";
//                    break;
//            }
//            current = current.Parent;
//        }

//        if (node != null)
//        {
//            syntaxKind = node.Kind().ToString();
//        }

//        var memberParts = new List<string>();
//        if (!string.IsNullOrEmpty(namespaceName))
//            memberParts.Add(namespaceName);
//        if (!string.IsNullOrEmpty(className))
//            memberParts.Add(className);
//        if (!string.IsNullOrEmpty(methodName))
//            memberParts.Add(methodName);
//        else if (!string.IsNullOrEmpty(propertyName))
//            memberParts.Add(propertyName);
//        else if (!string.IsNullOrEmpty(fieldName))
//            memberParts.Add(fieldName);

//        containingMember = memberParts.Count > 0 ? string.Join(".", memberParts) : "Global";

//        return new CodeContext
//        {
//            ClassName = className,
//            MethodName = methodName,
//            PropertyName = propertyName,
//            FieldName = fieldName,
//            NamespaceName = namespaceName,
//            SyntaxKind = syntaxKind,
//            ContainingMember = containingMember
//        };
//    }

//    static SourceText GetLineText(SyntaxTree syntaxTree, TextSpan textSpan)
//    {
//        var sourceText = syntaxTree.GetText();
//        var linePositionSpan = sourceText.Lines.GetLinePositionSpan(textSpan);
//        var lineSpan = sourceText.Lines.GetTextSpan(linePositionSpan);
//        return sourceText.GetSubText(lineSpan);
//    }
//}
