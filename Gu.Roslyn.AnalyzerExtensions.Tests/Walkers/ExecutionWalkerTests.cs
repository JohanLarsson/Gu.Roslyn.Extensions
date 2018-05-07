namespace Gu.Roslyn.AnalyzerExtensions.Tests.Walkers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    public class ExecutionWalkerTests
    {
        [TestCase(Scope.Member)]
        [TestCase(Scope.Instance)]
        [TestCase(Scope.Recursive)]
        public void SimpleCtor(Scope scope)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo()
        {
            var i = 1;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FindConstructorDeclaration("Foo");
            using (var walker = LiteralWalker.Borrow(node, scope, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual("1", walker.Literals.Single().ToString());
            }
        }

        [TestCase(Scope.Member, "1, 3")]
        [TestCase(Scope.Instance, "1, 2, 3")]
        [TestCase(Scope.Recursive, "1, 2, 3")]
        public void ChainedCtor(Scope scope, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo()
            : this(1)
        {
            var j = 3;
        }

        public Foo(int _)
        {
            var i = 2;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FindConstructorDeclaration("Foo");
            using (var walker = LiteralWalker.Borrow(node, scope, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual(expected, string.Join(", ", walker.Literals));
            }
        }

        [TestCase(Scope.Member, "2")]
        [TestCase(Scope.Instance, "1, 2")]
        [TestCase(Scope.Recursive, "1, 2")]
        public void ImplicitBaseCtor(Scope scope, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class FooBase
    {
        public FooBase()
        {
            var i = 1;
        }
    }

    public class Foo : FooBase
    {
        public Foo()
        {
            var j = 2;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FindConstructorDeclaration("public Foo()");
            using (var walker = LiteralWalker.Borrow(node, scope, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual(expected, string.Join(", ", walker.Literals));
            }
        }

        [TestCase(Scope.Member, "")]
        [TestCase(Scope.Instance, "1, 2")]
        [TestCase(Scope.Recursive, "1, 2")]
        public void AssignmentSetterWithGetter(Scope scope, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class Foo
    {
        private int value;

        public Foo()
        {
            Value = Value;
        }

        public int Value
        {
            get => 1;
            set => value = 2;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FindConstructorDeclaration("Foo");
            using (var walker = LiteralWalker.Borrow(node, scope, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual(expected, string.Join(", ", walker.Literals));
            }
        }

        [TestCase(Scope.Member, "")]
        [TestCase(Scope.Instance, "1, 2")]
        [TestCase(Scope.Recursive, "1, 2")]
        public void AssignmentSetterWithGetterThis(Scope scope, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class Foo
    {
        private int value;

        public Foo()
        {
            this.Value = this.Value;
        }

        public int Value
        {
            get => 1;
            set => this.value = 2;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FindConstructorDeclaration("Foo");
            using (var walker = LiteralWalker.Borrow(node, scope, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual(expected, string.Join(", ", walker.Literals));
            }
        }

        [TestCase(Scope.Member, "2, 3")]
        [TestCase(Scope.Instance, "1, 2, 3")]
        [TestCase(Scope.Recursive, "1, 2, 3")]
        public void ExpressionBodyAsArgument(Scope scope, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo()
        {
            Equals(this.Value, 2);
            int j = 3;
        }

        public int Value => 1;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FindConstructorDeclaration("Foo");
            using (var walker = LiteralWalker.Borrow(node, scope, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual(expected, string.Join(", ", walker.Literals));
            }
        }

        [TestCase(Scope.Member, "2, 3")]
        [TestCase(Scope.Instance, "1, 2, 3")]
        [TestCase(Scope.Recursive, "1, 2, 3")]
        public void InvocationAsArgument(Scope scope, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo()
        {
            Equals(this.Value(), 2);
            int j = 3;
        }

        public int Value() => 1;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FindConstructorDeclaration("Foo");
            using (var walker = LiteralWalker.Borrow(node, scope, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual(expected, string.Join(", ", walker.Literals));
            }
        }

        [TestCase(Scope.Member, "1, 3")]
        [TestCase(Scope.Instance, "1, 2, 3")]
        [TestCase(Scope.Recursive, "1, 2, 3")]
        public void InvocationVirtual(Scope scope, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    using System;

    public class Foo : IDisposable
    {
        public void Dispose()
        {
            var i = 1;
            Dispose(true);
            i = 3;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                var j = 2;
            }
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FindMethodDeclaration("public void Dispose()");
            using (var walker = LiteralWalker.Borrow(node, scope, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual(expected, string.Join(", ", walker.Literals));
            }
        }

        [TestCase(Scope.Member, "2")]
        [TestCase(Scope.Instance, "1, 2")]
        [TestCase(Scope.Recursive, "1, 2")]
        public void LocalDeclarationWithExpressionBody(Scope scope, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo()
        {
            var value = this.Value;
            value = 2;
        }

        public int Value => 1;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FindConstructorDeclaration("Foo");
            using (var walker = LiteralWalker.Borrow(node, scope, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual(expected, string.Join(", ", walker.Literals));
            }
        }

        [TestCase(Scope.Member, "2")]
        [TestCase(Scope.Instance, "1, 2")]
        [TestCase(Scope.Recursive, "1, 2")]
        public void LocalDeclarationWithCastExpressionBody(Scope scope, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo()
        {
            var value = (double)this.Value;
            value = 2;
        }

        public int Value => 1;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FindConstructorDeclaration("Foo");
            using (var walker = LiteralWalker.Borrow(node, scope, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual(expected, string.Join(", ", walker.Literals));
            }
        }

        [TestCase(Scope.Member, "1, 2")]
        [TestCase(Scope.Instance, "1, 2")]
        [TestCase(Scope.Recursive, "1, 2")]
        public void FieldInitializerBeforeCtor(Scope scope, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int value = 1;

        public Foo()
        {
            this.value = 2;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FindClassDeclaration("Foo");
            using (var walker = LiteralWalker.Borrow(node, scope, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual(expected, string.Join(", ", walker.Literals));
            }
        }

        [TestCase(Scope.Member, "1, 2")]
        [TestCase(Scope.Instance, "1, 2")]
        [TestCase(Scope.Recursive, "1, 2")]
        public void FieldInitializerBeforeCtorWhenNotDocumentOrder(Scope scope, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo()
        {
            this.value = 2;
        }

        private readonly int value = 1;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FindClassDeclaration("Foo");
            using (var walker = LiteralWalker.Borrow(node, scope, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual(expected, string.Join(", ", walker.Literals));
            }
        }

        private class LiteralWalker : ExecutionWalker<LiteralWalker>
        {
            private readonly List<LiteralExpressionSyntax> literals = new List<LiteralExpressionSyntax>();

            public IReadOnlyList<LiteralExpressionSyntax> Literals => this.literals;

            public static LiteralWalker Borrow(SyntaxNode node, Scope scope, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                return BorrowAndVisit(node, scope, semanticModel, cancellationToken, () => new LiteralWalker());
            }

            public override void VisitLiteralExpression(LiteralExpressionSyntax node)
            {
                if (node.IsKind(SyntaxKind.NumericLiteralExpression))
                {
                    this.literals.Add(node);
                }

                base.VisitLiteralExpression(node);
            }

            protected override void Clear()
            {
                this.literals.Clear();
                base.Clear();
            }
        }
    }
}