namespace Gu.Roslyn.AnalyzerExtensions.Tests.Symbols.KnownSymbol
{
    using System;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static class QualifiedTypeTests
    {
        [Test]
        public static void SymbolEquality()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(
                @"
namespace N
{
    internal class C
    {
        internal object M()
        {
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FindMethodDeclaration("M");
            var typeSymbol = semanticModel.GetDeclaredSymbol(node).ReturnType;
            Assert.AreEqual(true, typeSymbol == new QualifiedType("System.Object"));
            Assert.AreEqual(true, typeSymbol == QualifiedType.System.Object);
            Assert.AreEqual(false, typeSymbol == QualifiedType.System.String);
            Assert.AreEqual(false, typeSymbol != new QualifiedType("System.Object"));
            Assert.AreEqual(true, typeSymbol.IsAssignableTo(QualifiedType.System.Object, compilation));
            Assert.AreEqual(true, typeSymbol.IsSameType(QualifiedType.System.Object, compilation));
            Assert.AreEqual(false, typeSymbol.IsAssignableTo(QualifiedType.System.String, compilation));
            Assert.AreEqual(false, typeSymbol.IsSameType(QualifiedType.System.String, compilation));
        }

        [TestCase(typeof(object), "Object", "String")]
        [TestCase(typeof(object), "System.Object", "System.String")]
        [TestCase(typeof(object), "object", "string")]
        [TestCase(typeof(int?), "int?", "string")]
        [TestCase(typeof(int?), "int?", "double?")]
        [TestCase(typeof(int[]), "int[]", "double")]
        [TestCase(typeof(IComparable<int>), "IComparable<int>", "string")]
        [TestCase(typeof(IComparable<int>), "System.IComparable<int>", "string")]
        public static void TypeSyntaxEquality(Type type, string typeText, string otherTypeText)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(
                @"
namespace N
{
    using System;

    internal class C
    {
        internal object M() { }
        internal string Other() { }
    }
}".AssertReplace("object", typeText)
  .AssertReplace("string", otherTypeText));
            var typeSyntax = syntaxTree.FindMethodDeclaration("M").ReturnType;
            var qualifiedType = QualifiedType.FromType(type);
            Assert.AreEqual(true, typeSyntax == qualifiedType);
            Assert.AreEqual(false, typeSyntax != qualifiedType);

            typeSyntax = syntaxTree.FindMethodDeclaration("Other").ReturnType;
            Assert.AreEqual(false, typeSyntax == qualifiedType);
            Assert.AreEqual(true, typeSyntax != qualifiedType);
        }

        [TestCase("[Obsolete]")]
        [TestCase("[ObsoleteAttribute]")]
        [TestCase("[System.Obsolete]")]
        [TestCase("[System.ObsoleteAttribute]")]
        public static void TypeSyntaxEqualityAttribute(string attribute)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(
                @"
namespace N
{
    using System;

    [Obsolete]
    internal class C
    {
    }
}".AssertReplace("[Obsolete]", attribute));
            var typeSyntax = syntaxTree.FindAttribute(attribute).Name;
            var qualifiedType = QualifiedType.FromType(typeof(ObsoleteAttribute));
            Assert.AreEqual(true, typeSyntax == qualifiedType);
            Assert.AreEqual(false, typeSyntax != qualifiedType);
            Assert.AreEqual(true, typeSyntax == QualifiedType.System.ObsoleteAttribute);
            Assert.AreEqual(false, typeSyntax != QualifiedType.System.ObsoleteAttribute);
        }

        [Test]
        public static void TypeSyntaxEqualityWhenAlias()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(
                @"
namespace N
{
    using Aliased = System.ObsoleteAttribute;

    [Aliased]
    internal class C
    {
    }
}");
            var typeSyntax = syntaxTree.FindAttribute("Aliased").Name;
            var qualifiedType = QualifiedType.FromType(typeof(ObsoleteAttribute));
            Assert.AreEqual(true, typeSyntax == qualifiedType);
            Assert.AreEqual(false, typeSyntax != qualifiedType);
        }
    }
}
