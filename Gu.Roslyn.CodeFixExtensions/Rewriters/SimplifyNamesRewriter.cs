namespace Gu.Roslyn.CodeFixExtensions
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Simplification;

    /// <summary>
    /// Adds <see cref="Simplifier.Annotation"/> to all <see cref="QualifiedNameSyntax"/> and <see cref="QualifiedCrefSyntax"/>.
    /// </summary>
    public class SimplifyNamesRewriter : CSharpSyntaxRewriter
    {
        /// <summary>The default instance.</summary>
        public static readonly SimplifyNamesRewriter Default = new SimplifyNamesRewriter();

        private SimplifyNamesRewriter()
            : base(true)
        {
        }

        /// <summary>
        /// Adds <see cref="Simplifier.Annotation"/> to all <see cref="QualifiedNameSyntax"/> and <see cref="QualifiedCrefSyntax"/>.
        /// </summary>
        /// <typeparam name="T">The node type.</typeparam>
        /// <param name="node">The <see cref="T"/>.</param>
        /// <returns><paramref name="node"/> with <see cref="Simplifier.Annotation"/>.</returns>
        public static T Simplify<T>(T node)
            where T : SyntaxNode
        {
            return (T)Default.Visit(node);
        }

        /// <inheritdoc />
        public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
        {
            return base.VisitQualifiedName(node).WithAdditionalAnnotations(Simplifier.Annotation);
        }

        /// <inheritdoc />
        public override SyntaxNode VisitQualifiedCref(QualifiedCrefSyntax node)
        {
            return base.VisitQualifiedCref(node).WithAdditionalAnnotations(Simplifier.Annotation);
        }
    }
}
