namespace Gu.Roslyn.AnalyzerExtensions.Tests.StyleCopComparers
{
    using System.Collections.Generic;
    using System.Linq;
    using Gu.Roslyn.AnalyzerExtensions.StyleCopComparers;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    public static class PropertyDeclarationComparerTests
    {
        private static readonly SyntaxTree SyntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public class C : IC
    {
        public static int PublicStaticGet { get; } = 1;

        public static int PublicStaticGet1 { get; } = PublicStaticGet;

        public static int PublicStaticExpressionBody => PublicStaticGet;

        public int PublicGet { get; }

        object IC.PublicGet => this.PublicGet;

        public int PublicExpressionBody1 => this.PublicGet;

        public int PublicStatementBody1
        {
            get { return this.PublicGet; }
        }

        public int PublicExpressionBody2 => this.PublicGet;

        public int PublicStatementBody2
        {
            get { return this.PublicGet; }
        }

        public int PublicGetPrivateSet1 { get; private set; }

        public int PublicGetPrivateSet2 { get; private set; }

        public int PublicGetInternalSet { get; internal set; }

        public int PublicGetSet { get; set; }

        object IC.PublicGetSet
        {
            get { return this.PublicGetSet; }
            set { this.PublicGetSet = (int)value; }
        }

        internal int InternalGet { get; }

        internal int InternalExpressionBody => this.InternalGet;

        internal int InternalGetPrivateSet { get; private set; }

        internal int InternalGetSet { get; set; }

        private int PrivateGet { get; }

        private int PrivateExpressionBody => this.InternalGet;

        private int PrivateGetSet { get; set; }
    }

    public interface IC
    {
        object PublicGet { get; }

        object PublicGetSet { get; set; }
    }
}");

        private static readonly IReadOnlyList<TestCaseData> TestCaseSource = CreateTestCases().ToArray();

        [TestCaseSource(nameof(TestCaseSource))]
        public static void Compare(PropertyDeclarationSyntax x, PropertyDeclarationSyntax y)
        {
            Assert.AreEqual(-1, PropertyDeclarationComparer.Compare(x, y));
            Assert.AreEqual(1, PropertyDeclarationComparer.Compare(y, x));
            Assert.AreEqual(0, PropertyDeclarationComparer.Compare(x, x));
            Assert.AreEqual(0, PropertyDeclarationComparer.Compare(y, y));
        }

        [TestCaseSource(nameof(TestCaseSource))]
        public static void MemberDeclarationComparerCompare(PropertyDeclarationSyntax x, PropertyDeclarationSyntax y)
        {
            Assert.AreEqual(-1, MemberDeclarationComparer.Compare(x, y));
            Assert.AreEqual(1, MemberDeclarationComparer.Compare(y, x));
            Assert.AreEqual(0, MemberDeclarationComparer.Compare(x, x));
            Assert.AreEqual(0, MemberDeclarationComparer.Compare(y, y));
        }

        [TestCase("public int Value { get; }", "public int Value => 1;")]
        [TestCase("public int Value => 1;", "public int Value { get; set; }")]
        [TestCase("public int Value { get; private set; }", "public int Value { get; set; }")]
        public static void NoSpan(string code1, string code2)
        {
            var x = (PropertyDeclarationSyntax)SyntaxFactory.ParseCompilationUnit(code1).Members.Single();
            var y = (PropertyDeclarationSyntax)SyntaxFactory.ParseCompilationUnit(code2).Members.Single();
            Assert.AreEqual(-1, PropertyDeclarationComparer.Compare(x, y));
            Assert.AreEqual(1, PropertyDeclarationComparer.Compare(y, x));
        }

        [Test]
        public static void InitializedWithOther()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    class C
    {
        public static int PublicStatic1 { get; } = PublicStatic2;
        public static int PublicStatic2 { get; } = 3;
    }
}");
            var x = syntaxTree.FindPropertyDeclaration("public static int PublicStatic1 { get; } = PublicStatic2");
            var y = syntaxTree.FindPropertyDeclaration("public static int PublicStatic2 { get; } = 3");
            Assert.AreEqual(1, PropertyDeclarationComparer.Compare(x, y));
            Assert.AreEqual(-1, PropertyDeclarationComparer.Compare(y, x));
            Assert.AreEqual(0, PropertyDeclarationComparer.Compare(x, x));
            Assert.AreEqual(0, PropertyDeclarationComparer.Compare(y, y));
        }

        public static IEnumerable<TestCaseData> CreateTestCases()
        {
            var c = SyntaxTree.FindClassDeclaration("C");
            foreach (var member1 in c.Members)
            {
                foreach (var member2 in c.Members)
                {
                    if (member1.SpanStart < member2.SpanStart)
                    {
                        yield return new TestCaseData(member1, member2);
                    }
                }
            }
        }
    }
}
