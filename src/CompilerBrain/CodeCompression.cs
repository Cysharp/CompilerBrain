//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;

//namespace CompilerBrain;

//public static class CodeCompression
//{
//    public static string RemoveBody(SyntaxTree syntaxTree)
//    {
//        var root = syntaxTree.GetRoot();
//        var newNode = new BodyRemovalRewriter().Visit(root);
//        var normalized = newNode.NormalizeWhitespace(); // cleanup
//        return normalized.ToFullString();
//    }

//    sealed class BodyRemovalRewriter : CSharpSyntaxRewriter
//    {
//        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
//        {
//            if (node.Body != null)
//            {
//                var body = node.WithBody(null).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
//                var triviaRemoved = body.ReplaceTrivia([.. body.GetLeadingTrivia(), .. body.GetTrailingTrivia()], (x, y) =>
//                {
//                    return VisitTrivia(x);
//                });
//                return triviaRemoved;
//            }

//            return base.VisitMethodDeclaration(node);
//        }

//        public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
//        {
//            if (node.Body != null)
//            {
//                var body = node.WithBody(null).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
//                var triviaRemoved = body.ReplaceTrivia([.. body.GetLeadingTrivia(), .. body.GetTrailingTrivia()], (x, y) =>
//                {
//                    return VisitTrivia(x);
//                });
//                return triviaRemoved;
//            }
//            return base.VisitConstructorDeclaration(node);
//        }

//        public override SyntaxNode? VisitAccessorDeclaration(AccessorDeclarationSyntax node)
//        {
//            if (node.Body != null)
//            {
//                var body = node.WithBody(null).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
//                var triviaRemoved = body.ReplaceTrivia([.. body.GetLeadingTrivia(), .. body.GetTrailingTrivia()], (x, y) =>
//                {
//                    return VisitTrivia(x);
//                });
//                return triviaRemoved;
//            }
//            return base.VisitAccessorDeclaration(node);
//        }

//        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
//        {
//            switch (trivia.Kind())
//            {
//                case SyntaxKind.WhitespaceTrivia:
//                    break; // need whitespace

//                case SyntaxKind.SingleLineCommentTrivia:
//                    return default;
//                case SyntaxKind.MultiLineCommentTrivia:
//                case SyntaxKind.DocumentationCommentExteriorTrivia:
//                case SyntaxKind.SingleLineDocumentationCommentTrivia:
//                case SyntaxKind.MultiLineDocumentationCommentTrivia:
//                case SyntaxKind.RegionDirectiveTrivia:
//                case SyntaxKind.EndRegionDirectiveTrivia:
//                    return default; // removes

//                // other trivia keeps.
//                case SyntaxKind.EndOfLineTrivia:
//                    break;
//                case SyntaxKind.DisabledTextTrivia:
//                    break; // code in DisableTextTrivia is keeped?
//                case SyntaxKind.PreprocessingMessageTrivia:
//                    break;
//                case SyntaxKind.IfDirectiveTrivia:
//                    break;
//                case SyntaxKind.ElifDirectiveTrivia:
//                    break;
//                case SyntaxKind.ElseDirectiveTrivia:
//                    break;
//                case SyntaxKind.EndIfDirectiveTrivia:
//                    break;
//                case SyntaxKind.DefineDirectiveTrivia:
//                    break;
//                case SyntaxKind.UndefDirectiveTrivia:
//                    break;
//                case SyntaxKind.ErrorDirectiveTrivia:
//                    break;
//                case SyntaxKind.WarningDirectiveTrivia:
//                    break;
//                case SyntaxKind.LineDirectiveTrivia:
//                    break;
//                case SyntaxKind.PragmaWarningDirectiveTrivia:
//                    break;
//                case SyntaxKind.PragmaChecksumDirectiveTrivia:
//                    break;
//                case SyntaxKind.ReferenceDirectiveTrivia:
//                    break;
//                case SyntaxKind.BadDirectiveTrivia:
//                    break;
//                case SyntaxKind.SkippedTokensTrivia:
//                    break;
//                case SyntaxKind.ConflictMarkerTrivia:
//                    break;
//                case SyntaxKind.ShebangDirectiveTrivia:
//                    break;
//                case SyntaxKind.LoadDirectiveTrivia:
//                    break;
//                case SyntaxKind.NullableDirectiveTrivia:
//                    break;
//                case SyntaxKind.LineSpanDirectiveTrivia:
//                    break;
//                case SyntaxKind.IgnoredDirectiveTrivia:
//                    break;
//                default:
//                    break;
//            }

//            return trivia;
//        }
//    }
//}
