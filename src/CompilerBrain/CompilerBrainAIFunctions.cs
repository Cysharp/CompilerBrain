using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using ZLinq;

namespace CompilerBrain;

public class CompilerBrainAIFunctions(SessionMemory memory)
{
    public IEnumerable<AIFunction> GetAIFunctions()
    {
        yield return AIFunctionFactory.Create(GetProjects);
        yield return AIFunctionFactory.Create(GetDiagnostics);
        yield return AIFunctionFactory.Create(ReadImportantInformationFiles);
        yield return AIFunctionFactory.Create(ReadCode);
        yield return AIFunctionFactory.Create(ReadManyCodes);
        yield return AIFunctionFactory.Create(AddOrReplaceCode);
        yield return AIFunctionFactory.Create(SaveChangedCodeToDisc);
        yield return AIFunctionFactory.Create(SearchFiles);
        yield return AIFunctionFactory.Create(SearchCodeByRegex);
    }

    [Description("Get project names of loaded solution.")]
    public string[] GetProjects()
    {
        return memory.Projects.Select(x => x.Project.Name).ToArray(); // TODO: directory path?
    }

    [Description("Get error diagnostics of the target project.")]
    public CodeDiagnostic[] GetDiagnostics([Description("Project Name.")] string projectName)
    {
        var diagnostics = memory.GetCompilation(projectName).GetDiagnostics();
        return CodeDiagnostic.Errors(diagnostics);
    }

    [Description("Read important information files(ReadMe, Directory.Build.props, .editorconfig).")]
    public RootFile ReadImportantInformationFiles()
    {
        var rootFile = new RootFile();
        var solutionPath = Directory.GetDirectoryRoot(memory.Solution.FilePath!);
        foreach (var item in Directory.EnumerateFiles(solutionPath))
        {
            if (Path.GetFileNameWithoutExtension(item.AsSpan()).Equals("ReadMe", StringComparison.OrdinalIgnoreCase))
            {
                rootFile.ReadMe = File.ReadAllText(item);
            }
            else if (item.Equals("Directory.Build.props", StringComparison.OrdinalIgnoreCase))
            {
                rootFile.DirectoryBuildProps = File.ReadAllText(item);
            }
            else if (item.Equals(".editorconfig", StringComparison.OrdinalIgnoreCase))
            {
                rootFile.EditorConfig = File.ReadAllText(item);
            }
        }
        return rootFile;
    }

    [Description("Read existing code in current session context, if not found returns null.")]
    public string? ReadCode(string projectName, string fileNameOrFullPath)
    {
        var compilation = memory.GetCompilation(projectName);

        // NOTE: This code cannot distinguish between hierarchies.
        // If files with the same name exist, the first one will be selected.
        if (!compilation.SyntaxTrees.TryGetByName(fileNameOrFullPath, out var existingTree) || !existingTree.TryGetText(out var text))
        {
            return null;
        }

        return text.ToString();
    }

    [Description("Read existing code in current session context, if not found returns null.")]
    public ReadManyCodesResult[] ReadManyCodes(string projectName, string[] fileNameOrFullPaths)
    {
        var compilation = memory.GetCompilation(projectName);

        var result = new List<ReadManyCodesResult>(fileNameOrFullPaths.Length);
        foreach (var item in fileNameOrFullPaths)
        {
            if (compilation.SyntaxTrees.TryGetByName(item, out var tree) && tree.TryGetText(out var text))
            {
                result.Add(new(tree.FilePath, text.ToString()));
            }
        }

        return result.ToArray();
    }

    [Description("Add or replace new code to current session context, returns diagnostics of compile result.")]
    public AddOrReplaceResult AddOrReplaceCode(string projectName, Codes[] codes)
    {
        var (project, compilation) = memory.GetProjectAndCompilation(projectName);
        var parseOptions = (CSharpParseOptions?)project.ParseOptions ?? CSharpParseOptions.Default;

        if (codes.Length == 0)
        {
            return new AddOrReplaceResult { CodeChanges = [], Diagnostics = [] };
        }

        List<CodeChange> codeChanges = new(codes.Length);
        foreach (var item in codes)
        {
            var code = item.Code;
            var filePath = item.FileFullPath;

            if (compilation.SyntaxTrees.TryGetByName(filePath, out var oldTree))
            {
                var lineBreak = oldTree.GetLineBreakFromFirstLine();
                code = code.ReplaceLineEndings(lineBreak);

                var newTree = oldTree.WithChangedText(SourceText.From(code));
                var changes = newTree.GetChanges(oldTree);

                var lineChanges = new LineChanges[changes.Count];
                var i = 0;
                foreach (var change in changes)
                {
                    var changeText = GetLineText(oldTree, change.Span);
                    lineChanges[i++] = new LineChanges { RemoveLine = changeText.ToString(), AddLine = change.NewText };
                }

                codeChanges.Add(new CodeChange { FilePath = filePath, LineChanges = lineChanges });
                compilation = compilation.ReplaceSyntaxTree(oldTree, newTree);
            }
            else
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code, options: parseOptions, path: filePath);
                codeChanges.Add(new CodeChange { FilePath = filePath, LineChanges = [new LineChanges { RemoveLine = null, AddLine = code }] });
                compilation = compilation.AddSyntaxTrees(syntaxTree);
            }
        }

        var diagnostics = CodeDiagnostic.Errors(compilation.GetDiagnostics());

        var result = new AddOrReplaceResult
        {
            CodeChanges = codeChanges.ToArray(),
            Diagnostics = diagnostics
        };

        if (diagnostics.Length == 0)
        {
            memory.SetChangedCodes(projectName, compilation, codeChanges.Select(x => x.FilePath).ToArray());
        }

        return result;
    }

    [Description("Save all add/modified codes in current in-memory session context, return value is saved paths.")]
    public string[] SaveChangedCodeToDisc()
    {
        var changed = memory.FlushChangedCodes();
        if (changed == null) return [];

        var compilation = changed.Value.Compilation;
        var changedFilePaths = changed.Value.ChangedFilePaths;

        var savedPaths = new List<string>(changedFilePaths.Length);
        foreach (var filePath in changedFilePaths)
        {
            if (compilation.SyntaxTrees.TryGetByName(filePath, out var tree))
            {
                if (tree.TryGetText(out var text))
                {
                    File.WriteAllText(tree.FilePath, text.ToString(), tree.Encoding ?? Encoding.UTF8);
                    savedPaths.Add(tree.FilePath);
                }
            }
        }

        return savedPaths.ToArray();
    }

    [Description("Search file-list using regular expressions.")]
    public string[] SearchFiles(string projectName, string targetFileRegex)
    {
        var compilation = memory.GetCompilation(projectName);

        // accept user generated regex patterns so no-compiled and non-backtracking options are used.
        var targetFilePattern = new Regex(targetFileRegex, RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

        var matches = new List<SearchMatch>();

        var targetTrees = compilation.SyntaxTrees
            .Where(tree => !string.IsNullOrEmpty(tree.FilePath) &&
                          File.Exists(tree.FilePath) &&
                          targetFilePattern.IsMatch(Path.GetFileName(tree.FilePath)));

        return targetTrees.Select(t => t.FilePath).ToArray();
    }

    // TODO: file path should use glob pattern?

    [Description("Search for code patterns using regular expressions in files matching the target file pattern.")]
    public SearchResult SearchCodeByRegex(string projectName, string targetFileRegex, string searchRegex)
    {
        var compilation = memory.GetCompilation(projectName);

        // accept user generated regex patterns so no-compiled and non-backtracking options are used.
        var targetFilePattern = new Regex(targetFileRegex, RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);
        var searchPattern = new Regex(searchRegex, RegexOptions.Multiline | RegexOptions.NonBacktracking);

        var matches = new List<SearchMatch>();

        var targetTrees = compilation.SyntaxTrees
            .Where(tree => !string.IsNullOrEmpty(tree.FilePath) &&
                          File.Exists(tree.FilePath) &&
                          targetFilePattern.IsMatch(Path.GetFileName(tree.FilePath)));

        foreach (var syntaxTree in targetTrees)
        {
            if (syntaxTree.TryGetText(out var sourceText))
            {
                var fullText = sourceText.ToString();
                var regexMatches = searchPattern.Matches(fullText);
                var root = syntaxTree.GetRoot();

                foreach (Match match in regexMatches)
                {
                    var textSpan = new TextSpan(match.Index, match.Length);
                    var linePosition = sourceText.Lines.GetLinePosition(match.Index);
                    var lineText = sourceText.Lines[linePosition.Line].ToString();

                    var context = AnalyzeSyntaxContext(root, match.Index);

                    matches.Add(new SearchMatch
                    {
                        FilePath = syntaxTree.FilePath,
                        LineNumber = linePosition.Line + 1,
                        ColumnNumber = linePosition.Character + 1,
                        LineText = lineText,
                        MatchedText = match.Value,
                        Location = new CodeLocation(match.Index, match.Length),
                        Context = context
                    });
                }
            }
        }

        return new SearchResult
        {
            Matches = matches.ToArray(),
            TotalMatches = matches.Count
        };
    }

    static SourceText GetLineText(SyntaxTree syntaxTree, TextSpan textSpan)
    {
        var sourceText = syntaxTree.GetText();
        var linePositionSpan = sourceText.Lines.GetLinePositionSpan(textSpan);
        var lineSpan = sourceText.Lines.GetTextSpan(linePositionSpan);
        return sourceText.GetSubText(lineSpan);
    }

    static CodeContext AnalyzeSyntaxContext(SyntaxNode root, int position)
    {
        var node = root.FindToken(position).Parent;

        string? className = null;
        string? methodName = null;
        string? propertyName = null;
        string? fieldName = null;
        string? namespaceName = null;
        string syntaxKind = "Unknown";
        string containingMember = "Global";

        var current = node;
        while (current != null)
        {
            switch (current)
            {
                case NamespaceDeclarationSyntax ns:
                    namespaceName = ns.Name.ToString();
                    break;
                case FileScopedNamespaceDeclarationSyntax fileNs:
                    namespaceName = fileNs.Name.ToString();
                    break;
                case ClassDeclarationSyntax cls:
                    className = cls.Identifier.ValueText;
                    break;
                case RecordDeclarationSyntax record:
                    className = record.Identifier.ValueText + " (record)";
                    break;
                case StructDeclarationSyntax str:
                    className = str.Identifier.ValueText + " (struct)";
                    break;
                case InterfaceDeclarationSyntax iface:
                    className = iface.Identifier.ValueText + " (interface)";
                    break;
                case MethodDeclarationSyntax method:
                    methodName = method.Identifier.ValueText;
                    break;
                case ConstructorDeclarationSyntax ctor:
                    methodName = ".ctor";
                    break;
                case PropertyDeclarationSyntax prop:
                    propertyName = prop.Identifier.ValueText;
                    break;
                case FieldDeclarationSyntax field when field.Declaration.Variables.Count > 0:
                    fieldName = field.Declaration.Variables[0].Identifier.ValueText;
                    break;
                case LocalFunctionStatementSyntax localFunc:
                    methodName = localFunc.Identifier.ValueText + " (local)";
                    break;
            }
            current = current.Parent;
        }

        if (node != null)
        {
            syntaxKind = node.Kind().ToString();
        }

        var memberParts = new List<string>();
        if (!string.IsNullOrEmpty(namespaceName))
            memberParts.Add(namespaceName);
        if (!string.IsNullOrEmpty(className))
            memberParts.Add(className);
        if (!string.IsNullOrEmpty(methodName))
            memberParts.Add(methodName);
        else if (!string.IsNullOrEmpty(propertyName))
            memberParts.Add(propertyName);
        else if (!string.IsNullOrEmpty(fieldName))
            memberParts.Add(fieldName);

        containingMember = memberParts.Count > 0 ? string.Join(".", memberParts) : "Global";

        return new CodeContext
        {
            ClassName = className,
            MethodName = methodName,
            PropertyName = propertyName,
            FieldName = fieldName,
            NamespaceName = namespaceName,
            SyntaxKind = syntaxKind,
            ContainingMember = containingMember
        };
    }
}
