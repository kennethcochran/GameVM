using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;
using NUnit.Framework;

namespace GameVM.Compiler.Pascal.Tests
{
    [TestFixture]
    public class TransformationContextTests
    {
        [Test]
        public void Constructor_InitializesProperties()
        {
            var context = new TransformationContext("test.pas", new HighLevelIR { SourceFile = "test.pas" });

            Assert.That(context.SourceFile, Is.EqualTo("test.pas"));
            Assert.That(context.TypeCache, Is.Not.Null);
            Assert.That(context.SymbolTable, Is.Not.Null);
            Assert.That(context.FunctionScope, Is.Not.Null);
            Assert.That(context.Errors, Is.Not.Null);
            Assert.That(context.Errors, Is.Empty);
        }

        [Test]
        public void GetOrCreateBasicType_NewType_AddsToCache()
        {
            var context = new TransformationContext("test.pas", new HighLevelIR { SourceFile = "test.pas" });
            var typeName = "integer";

            var type = context.GetOrCreateBasicType(typeName);

            Assert.That(type, Is.Not.Null);
            Assert.That(type.Name, Is.EqualTo(typeName));
            Assert.That(context.TypeCache, Contains.Key(typeName));
            Assert.That(context.TypeCache[typeName], Is.SameAs(type));
        }

        [Test]
        public void GetOrCreateBasicType_ExistingType_ReturnsCached()
        {
            var context = new TransformationContext("test.pas", new HighLevelIR { SourceFile = "test.pas" });
            var typeName = "integer";
            var firstCall = context.GetOrCreateBasicType(typeName);

            var secondCall = context.GetOrCreateBasicType(typeName);

            Assert.That(secondCall, Is.SameAs(firstCall));
        }

        [Test]
        public void AddError_AddsToErrorList()
        {
            var context = new TransformationContext("test.pas", new HighLevelIR { SourceFile = "test.pas" });
            var errorMessage = "Something went wrong";

            context.AddError(errorMessage);

            Assert.That(context.Errors, Has.Count.EqualTo(1));
            Assert.That(context.Errors[0], Is.EqualTo(errorMessage));
        }
    }
}
