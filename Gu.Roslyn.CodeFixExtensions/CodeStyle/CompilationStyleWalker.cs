namespace Gu.Roslyn.CodeFixExtensions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Base class for walking trees in a compilation.
    /// </summary>
    /// <typeparam name="T">The walker type.</typeparam>
    public abstract class CompilationStyleWalker<T> : PooledWalker<T>
        where T : CompilationStyleWalker<T>
    {
        private CodeStyleResult result = CodeStyleResult.NotFound;

        /// <summary>
        /// Walk <paramref name="containing"/> then all documents in project.
        /// </summary>
        /// <param name="containing">The <see cref="Document"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that cancels the operation.</param>
        /// <returns>The <see cref="CodeStyleResult"/> found.</returns>
        protected async Task<CodeStyleResult> CheckCoreAsync(Document containing, CancellationToken cancellationToken)
        {
            if (containing == null)
            {
                throw new ArgumentNullException(nameof(containing));
            }

            if (await Check(containing).ConfigureAwait(false) is CodeStyleResult containingResult &&
                containingResult != CodeStyleResult.NotFound)
            {
                return containingResult;
            }

            foreach (var document in containing.Project.Documents)
            {
                if (document == containing)
                {
                    continue;
                }

                if (await Check(document).ConfigureAwait(false) is CodeStyleResult documentResult &&
                    documentResult != CodeStyleResult.NotFound)
                {
                    return documentResult;
                }
            }

            return CodeStyleResult.NotFound;

            async Task<CodeStyleResult> Check(Document candidate)
            {
                var tree = await candidate.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                if (IsExcluded(tree))
                {
                    return CodeStyleResult.NotFound;
                }

                if (tree.TryGetRoot(out var root))
                {
                    this.Visit(root);
                    return this.result;
                }

                return CodeStyleResult.NotFound;
            }
        }

        /// <summary>
        /// Walk <paramref name="containing"/> then trees in <paramref name="compilation"/>.
        /// </summary>
        /// <param name="containing">The <see cref="Document"/>.</param>
        /// <param name="compilation">The <see cref="Compilation"/>.</param>
        /// <returns>The <see cref="CodeStyleResult"/> found.</returns>
        protected CodeStyleResult CheckCore(SyntaxTree containing, Compilation compilation)
        {
            if (containing == null)
            {
                throw new ArgumentNullException(nameof(containing));
            }

            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            if (Check(containing) is CodeStyleResult containingResult &&
                containingResult != CodeStyleResult.NotFound)
            {
                return containingResult;
            }

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                if (syntaxTree == containing)
                {
                    continue;
                }

                if (Check(syntaxTree) is CodeStyleResult syntaxTreeResult &&
                    syntaxTreeResult != CodeStyleResult.NotFound)
                {
                    return syntaxTreeResult;
                }
            }

            return CodeStyleResult.NotFound;

            CodeStyleResult Check(SyntaxTree tree)
            {
                if (IsExcluded(tree))
                {
                    return CodeStyleResult.NotFound;
                }

                if (tree.TryGetRoot(out var root))
                {
                    this.Visit(root);
                    return this.result;
                }

                return CodeStyleResult.NotFound;
            }
        }

        /// <summary>
        /// Update the result field.
        /// </summary>
        /// <param name="newValue">The <see cref="CodeStyleResult"/>.</param>
        protected void Update(CodeStyleResult newValue)
        {
            switch (this.result)
            {
                case CodeStyleResult.NotFound:
                    this.result = newValue;
                    break;
                case CodeStyleResult.Yes:
                    if (newValue == CodeStyleResult.No)
                    {
                        this.result = CodeStyleResult.Mixed;
                    }

                    break;
                case CodeStyleResult.No:
                    if (newValue == CodeStyleResult.Yes)
                    {
                        this.result = CodeStyleResult.Mixed;
                    }

                    break;
                case CodeStyleResult.Mixed:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newValue), newValue, null);
            }
        }

        /// <inheritdoc />
        protected override void Clear()
        {
            this.result = CodeStyleResult.NotFound;
        }

        private static bool IsExcluded(SyntaxTree syntaxTree)
        {
            return syntaxTree.FilePath.EndsWith(".g.i.cs", StringComparison.Ordinal) ||
                   syntaxTree.FilePath.EndsWith(".g.cs", StringComparison.Ordinal);
        }
    }
}