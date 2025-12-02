using Anthropic.Models.Beta.Messages;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace CompilerBrain;

internal static class Extensions
{
    // by name or file-path
    internal static bool TryGetByName(this IEnumerable<SyntaxTree> syntaxTrees, string name, [MaybeNullWhen(false)] out SyntaxTree syntaxTree)
    {
        if (syntaxTrees is ImmutableArray<SyntaxTree> immutableArray) // faster-path
        {
            var array = ImmutableCollectionsMarshal.AsArray(immutableArray)!;
            foreach (var tree in array)
            {
                var filePath = tree.FilePath.AsSpan(); // don't allocate new string in Path methods

                // match full-path or with extension or without extension
                if (filePath.Equals(name, StringComparison.OrdinalIgnoreCase)
                  || Path.GetFileName(filePath).Equals(name, StringComparison.OrdinalIgnoreCase)
                  || Path.GetFileNameWithoutExtension(filePath).Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    syntaxTree = tree;
                    return true;
                }
            }
        }
        else
        {
            foreach (var tree in syntaxTrees)
            {
                var filePath = tree.FilePath.AsSpan();

                if (filePath.Equals(name, StringComparison.OrdinalIgnoreCase)
                  || Path.GetFileName(filePath).Equals(name, StringComparison.OrdinalIgnoreCase)
                  || Path.GetFileNameWithoutExtension(filePath).Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    syntaxTree = tree;
                    return true;
                }
            }
        }

        syntaxTree = null;
        return false;
    }

    internal static string GetLineBreakFromFirstLine(this SyntaxTree syntaxTree)
    {
        var text = syntaxTree.GetText();
        var lines = text.Lines;

        if (lines.Count == 0)
        {
            return Environment.NewLine;
        }

        var firstLine = lines[0];
        var span = firstLine.SpanIncludingLineBreak;
        int lineBreakLength = span.Length - firstLine.Span.Length;

        if (lineBreakLength > 0)
        {
            return text.GetSubText(new Microsoft.CodeAnalysis.Text.TextSpan(firstLine.End, lineBreakLength)).ToString();
        }

        for (int i = 1; i < lines.Count; i++)
        {
            var line = lines[i];
            span = line.SpanIncludingLineBreak;
            lineBreakLength = span.Length - line.Span.Length;

            if (lineBreakLength > 0)
            {
                return text.GetSubText(new Microsoft.CodeAnalysis.Text.TextSpan(line.End, lineBreakLength)).ToString();
            }
        }

        return Environment.NewLine;
    }
}
