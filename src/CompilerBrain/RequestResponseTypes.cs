//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace CompilerBrain;

//// sln/proj

//public readonly record struct ProjectNameAndFilePath(string ProjectName, string ProjectFilePath);



//public readonly record struct CodeLocation(int Start, int Length);

//public readonly record struct CodeStructure
//{
//    public required int Page { get; init; }
//    public required int TotalPage { get; init; }
//    public required AnalyzedCode[] Codes { get; init; }
//}

//public readonly record struct AnalyzedCode
//{
//    public required string FilePath { get; init; }
//    public required string CodeWithoutBody { get; init; }
//}

//public readonly record struct Codes
//{
//    public required string FilePath { get; init; }
//    public required string Code { get; init; }
//}

//// Insert code structures
//public readonly record struct InsertCodeRequest
//{
//    public required string FilePath { get; init; }
//    public required string CodeToInsert { get; init; }
//    public required int Position { get; init; }
//    public required InsertMode Mode { get; init; }
//}

//public enum InsertMode
//{
//    /// <summary>Insert at exact character position</summary>
//    AtPosition,
//    /// <summary>Insert at the beginning of the specified line (1-based)</summary>
//    AtLineStart,
//    /// <summary>Insert at the end of the specified line (1-based)</summary>
//    AtLineEnd,
//    /// <summary>Insert after the specified line (1-based)</summary>
//    AfterLine,
//    /// <summary>Insert before the specified line (1-based)</summary>
//    BeforeLine
//}

//public readonly record struct InsertCodeResult
//{
//    public required CodeChange[] CodeChanges { get; init; }
//    public required CodeDiagnostic[] Diagnostics { get; init; }
//}

//public readonly record struct AddOrReplaceResult
//{
//    public required CodeChange[] CodeChanges { get; init; }
//    public required CodeDiagnostic[] Diagnostics { get; init; }
//}

//public readonly record struct CodeChange
//{
//    public required string FilePath { get; init; }
//    public required LineChanges[] LineChanges { get; init; }
//}

//public readonly record struct LineChanges
//{
//    public required string? RemoveLine { get; init; }
//    public required string? AddLine { get; init; }

//    public override string ToString()
//    {
//        return (RemoveLine, AddLine) switch
//        {
//            (null, null) => "",
//            (var remove, null) => "-" + remove,
//            (null, var add) => "+" + add,
//            (var remove, var add) => "-" + remove + Environment.NewLine + "+" + add,
//        };
//    }
//}

//// Search result structures
//public readonly record struct SearchResult
//{
//    public required SearchMatch[] Matches { get; init; }
//    public required int TotalMatches { get; init; }
//}

//public readonly record struct SearchMatch
//{
//    public required string FilePath { get; init; }
//    public required int LineNumber { get; init; }
//    public required int ColumnNumber { get; init; }
//    public required string LineText { get; init; }
//    public required string MatchedText { get; init; }
//    public required CodeLocation Location { get; init; }
//    public required CodeContext Context { get; init; }
//}

//public readonly record struct CodeContext
//{
//    public required string? ClassName { get; init; }
//    public required string? MethodName { get; init; }
//    public required string? PropertyName { get; init; }
//    public required string? FieldName { get; init; }
//    public required string? NamespaceName { get; init; }
//    public required string SyntaxKind { get; init; }
//    public required string ContainingMember { get; init; }
//}

//// Symbol reference structures
//public readonly record struct SymbolReferenceResult
//{
//    public required SymbolInfo TargetSymbol { get; init; }
//    public required SymbolReference[] References { get; init; }
//    public required SymbolReference[] Implementations { get; init; }
//    public required SymbolReference[] Declarations { get; init; }
//    public required int TotalReferences { get; init; }
//    public required int TotalImplementations { get; init; }
//    public required int TotalDeclarations { get; init; }
//}

//public readonly record struct SymbolReference
//{
//    public required string FilePath { get; init; }
//    public required int LineNumber { get; init; }
//    public required int ColumnNumber { get; init; }
//    public required string LineText { get; init; }
//    public required string SymbolName { get; init; }
//    public required string SymbolKind { get; init; }
//    public required CodeLocation Location { get; init; }
//    public required CodeContext Context { get; init; }
//    public required string ReferenceKind { get; init; } // "Reference", "Implementation", "Declaration"
//}

//public readonly record struct SymbolInfo
//{
//    public required string Name { get; init; }
//    public required string FullName { get; init; }
//    public required string Kind { get; init; }
//    public required string ContainingNamespace { get; init; }
//    public required string ContainingType { get; init; }
//    public required string Assembly { get; init; }
//    public required bool IsGeneric { get; init; }
//    public required string[] TypeParameters { get; init; }
//    public required string Accessibility { get; init; }
//    public required bool IsAbstract { get; init; }
//    public required bool IsVirtual { get; init; }
//    public required bool IsSealed { get; init; }
//    public required bool IsStatic { get; init; }
//}

//public readonly record struct FindSymbolResult
//{
//    public required SymbolInfo[] Symbols { get; init; }
//    public required int TotalCount { get; init; }
//}

//// Solution diagnostic structures
//public readonly record struct SolutionDiagnostic
//{
//    public required string SolutionPath { get; init; }
//    public required ProjectDiagnostic[] ProjectDiagnostics { get; init; }
//    public required int TotalProjects { get; init; }
//    public required int CSharpProjects { get; init; }
//}

//public readonly record struct ProjectDiagnostic
//{
//    public required string ProjectName { get; init; }
//    public required string ProjectPath { get; init; }
//    public required CodeDiagnostic[] Diagnostics { get; init; }
//}
