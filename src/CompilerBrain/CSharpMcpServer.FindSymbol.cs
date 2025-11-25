//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.FindSymbols;
//using ModelContextProtocol.Server;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;

//namespace CompilerBrain;

//public static partial class CSharpMcpServer
//{

//    [McpServerTool, Description("Find symbols by name pattern in the current compilation. Use wildcards like 'I*' to find all interfaces starting with 'I'.")]
//    public static FindSymbolResult FindSymbolsByName(SessionMemory memory, Guid sessionId, string projectFilePath, string namePattern)
//    {
//        var compilation = memory.GetCompilation(sessionId, projectFilePath);

//        var regexPattern = "^" + Regex.Escape(namePattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
//        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

//        var foundSymbols = new List<ISymbol>();

//        var allSymbols = compilation.GetSymbolsWithName(name => regex.IsMatch(name), SymbolFilter.All);

//        foreach (var symbol in allSymbols)
//        {
//            if (!symbol.IsImplicitlyDeclared && symbol.Locations.Any(loc => loc.IsInSource))
//            {
//                foundSymbols.Add(symbol);
//            }
//        }

//        var symbolInfos = foundSymbols
//            .Select(CreateSymbolInfo)
//            .ToArray();

//        return new FindSymbolResult
//        {
//            Symbols = symbolInfos,
//            TotalCount = symbolInfos.Length
//        };
//    }

//    [McpServerTool, Description("Find all references to a symbol by its name and location. Returns references, implementations, and declarations.")]
//    public static async Task<SymbolReferenceResult> FindSymbolReferences(SessionMemory memory, Guid sessionId, string projectFilePath, string filePath, int position)
//    {
//        var session = memory.GetSession(sessionId);
//        var solution = session.Solution;
//        var compilation = solution.GetProject(projectFilePath).Compilation;

//        if (!compilation.SyntaxTrees.TryGet(filePath, out var syntaxTree))
//        {
//            throw new ArgumentException($"File path not found in compilation: {filePath}");
//        }

//        var root = await syntaxTree.GetRootAsync();
//        var semanticModel = compilation.GetSemanticModel(syntaxTree);

//        var token = root.FindToken(position);
//        var symbolInfo = semanticModel.GetSymbolInfo(token.Parent!);
//        var symbol = symbolInfo.Symbol;

//        if (symbol == null)
//        {
//            throw new ArgumentException($"No symbol found at position {position} in file {filePath}");
//        }

//        var references = await SymbolFinder.FindReferencesAsync(symbol, solution.RawSolution);
//        var allReferences = new List<SymbolReference>();

//        foreach (var reference in references)
//        {
//            foreach (var location in reference.Locations)
//            {
//                if (location.Location.IsInSource)
//                {
//                    var symbolRef = CreateSymbolReference(location.Location, reference.Definition, "Reference");
//                    if (symbolRef.HasValue)
//                        allReferences.Add(symbolRef.Value);
//                }
//            }
//        }

//        return new SymbolReferenceResult
//        {
//            TargetSymbol = CreateSymbolInfo(symbol),
//            References = allReferences.ToArray(),
//            Implementations = [],
//            Declarations = [],
//            TotalReferences = allReferences.Count,
//            TotalImplementations = 0,
//            TotalDeclarations = 0
//        };
//    }

//    [McpServerTool, Description("Find all types that implement a specific interface. Provide the interface name (e.g., 'IFoo').")]
//    public static async Task<SymbolReferenceResult> FindInterfaceImplementations(SessionMemory memory, Guid sessionId, string projectFilePath, string interfaceName)
//    {
//        var session = memory.GetSession(sessionId);
//        var solution = session.Solution;
//        var compilation = solution.GetProject(projectFilePath).Compilation;

//        var interfaceSymbol = compilation.GetSymbolsWithName(interfaceName, SymbolFilter.Type)
//            .OfType<ITypeSymbol>()
//            .FirstOrDefault(s => s.TypeKind == TypeKind.Interface);

//        if (interfaceSymbol == null)
//        {
//            throw new ArgumentException($"Interface '{interfaceName}' not found in compilation.");
//        }

//        if (interfaceSymbol is not INamedTypeSymbol namedInterfaceSymbol)
//        {
//            throw new ArgumentException($"Interface '{interfaceName}' is not a named type symbol.");
//        }

//        var implementations = await SymbolFinder.FindImplementationsAsync(namedInterfaceSymbol, solution.RawSolution);
//        var allImplementations = new List<SymbolReference>();

//        foreach (var impl in implementations)
//        {
//            foreach (var location in impl.Locations.Where(loc => loc.IsInSource))
//            {
//                var symbolRef = CreateSymbolReference(location, impl, "Implementation");
//                if (symbolRef.HasValue)
//                    allImplementations.Add(symbolRef.Value);
//            }
//        }

//        return new SymbolReferenceResult
//        {
//            TargetSymbol = CreateSymbolInfo(interfaceSymbol),
//            References = [],
//            Implementations = allImplementations.ToArray(),
//            Declarations = [],
//            TotalReferences = 0,
//            TotalImplementations = allImplementations.Count,
//            TotalDeclarations = 0
//        };
//    }

//    [McpServerTool, Description("Find all derived classes of a specific base class. Provide the base class name (e.g., 'BaseClass').")]
//    public static async Task<SymbolReferenceResult> FindDerivedClasses(SessionMemory memory, Guid sessionId, string projectFilePath, string baseClassName)
//    {
//        var session = memory.GetSession(sessionId);
//        var solution = session.Solution;
//        var compilation = solution.GetProject(projectFilePath).Compilation;

//        var baseClassSymbol = compilation.GetSymbolsWithName(baseClassName, SymbolFilter.Type)
//            .OfType<ITypeSymbol>()
//            .FirstOrDefault(s => s.TypeKind == TypeKind.Class);

//        if (baseClassSymbol == null)
//        {
//            throw new ArgumentException($"Base class '{baseClassName}' not found in compilation.");
//        }

//        if (baseClassSymbol is not INamedTypeSymbol namedBaseClassSymbol)
//        {
//            throw new ArgumentException($"Base class '{baseClassName}' is not a named type symbol.");
//        }

//        var derivedClasses = await SymbolFinder.FindDerivedClassesAsync(namedBaseClassSymbol, solution.RawSolution);
//        var allDerived = new List<SymbolReference>();

//        foreach (var derived in derivedClasses)
//        {
//            foreach (var location in derived.Locations.Where(loc => loc.IsInSource))
//            {
//                var symbolRef = CreateSymbolReference(location, derived, "Derived");
//                if (symbolRef.HasValue)
//                    allDerived.Add(symbolRef.Value);
//            }
//        }

//        return new SymbolReferenceResult
//        {
//            TargetSymbol = CreateSymbolInfo(baseClassSymbol),
//            References = [],
//            Implementations = allDerived.ToArray(),
//            Declarations = [],
//            TotalReferences = 0,
//            TotalImplementations = allDerived.Count,
//            TotalDeclarations = 0
//        };
//    }

//    static SymbolReference? CreateSymbolReference(Location location, ISymbol symbol, string referenceKind)
//    {
//        if (!location.IsInSource || location.SourceTree == null)
//            return null;

//        var sourceText = location.SourceTree.GetText();
//        var linePosition = sourceText.Lines.GetLinePosition(location.SourceSpan.Start);
//        var lineText = sourceText.Lines[linePosition.Line].ToString();
//        var root = location.SourceTree.GetRoot();
//        var context = AnalyzeSyntaxContext(root, location.SourceSpan.Start);

//        return new SymbolReference
//        {
//            FilePath = location.SourceTree.FilePath,
//            LineNumber = linePosition.Line + 1,
//            ColumnNumber = linePosition.Character + 1,
//            LineText = lineText,
//            SymbolName = symbol.Name,
//            SymbolKind = symbol.Kind.ToString(),
//            Location = new CodeLocation(location.SourceSpan.Start, location.SourceSpan.Length),
//            Context = context,
//            ReferenceKind = referenceKind
//        };
//    }

//    static SymbolInfo CreateSymbolInfo(ISymbol symbol)
//    {
//        var typeParameters = symbol is INamedTypeSymbol namedType && namedType.IsGenericType
//            ? namedType.TypeParameters.Select(tp => tp.Name).ToArray()
//            : [];

//        return new SymbolInfo
//        {
//            Name = symbol.Name,
//            FullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
//            Kind = symbol.Kind.ToString(),
//            ContainingNamespace = symbol.ContainingNamespace?.ToDisplayString() ?? "",
//            ContainingType = symbol.ContainingType?.Name ?? "",
//            Assembly = symbol.ContainingAssembly?.Name ?? "",
//            IsGeneric = symbol is INamedTypeSymbol { IsGenericType: true },
//            TypeParameters = typeParameters,
//            Accessibility = symbol.DeclaredAccessibility.ToString(),
//            IsAbstract = symbol.IsAbstract,
//            IsVirtual = symbol.IsVirtual,
//            IsSealed = symbol.IsSealed,
//            IsStatic = symbol.IsStatic
//        };
//    }
//}
