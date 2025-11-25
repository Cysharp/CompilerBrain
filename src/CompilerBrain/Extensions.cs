//using Microsoft.CodeAnalysis;
//using ModelContextProtocol;
//using System.Collections.Immutable;
//using System.Diagnostics.CodeAnalysis;

//namespace CompilerBrain;

//internal static class Extensions
//{
//    internal static bool TryGet(this IEnumerable<SyntaxTree> syntaxTrees, string filePath, [MaybeNullWhen(false)] out SyntaxTree syntaxTree)
//    {
//        if (syntaxTrees is ImmutableArray<SyntaxTree> immutableArray)
//        {
//            foreach (var tree in immutableArray) // faster iteration
//            {
//                if (tree.FilePath == filePath)
//                {
//                    syntaxTree = tree;
//                    return true;
//                }
//            }
//        }
//        else
//        {
//            foreach (var tree in syntaxTrees)
//            {
//                if (tree.FilePath == filePath)
//                {
//                    syntaxTree = tree;
//                    return true;
//                }
//            }
//        }

//        syntaxTree = null;
//        return false;
//    }

//    internal static string GetLineBreakFromFirstLine(this SyntaxTree syntaxTree)
//    {
//        var text = syntaxTree.GetText();
//        var lines = text.Lines;

//        if (lines.Count == 0)
//        {
//            return Environment.NewLine;
//        }

//        var firstLine = lines[0];
//        var span = firstLine.SpanIncludingLineBreak;
//        int lineBreakLength = span.Length - firstLine.Span.Length;

//        if (lineBreakLength > 0)
//        {
//            return text.GetSubText(new Microsoft.CodeAnalysis.Text.TextSpan(firstLine.End, lineBreakLength)).ToString();
//        }

//        for (int i = 1; i < lines.Count; i++)
//        {
//            var line = lines[i];
//            span = line.SpanIncludingLineBreak;
//            lineBreakLength = span.Length - line.Span.Length;

//            if (lineBreakLength > 0)
//            {
//                return text.GetSubText(new Microsoft.CodeAnalysis.Text.TextSpan(line.End, lineBreakLength)).ToString();
//            }
//        }

//        return Environment.NewLine;
//    }
//}
