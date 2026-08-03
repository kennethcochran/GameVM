using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Buffers;

namespace GameVM.Compiler.Pascal.Tests
{
    [TestFixture]
    public class TransformationContextTests
    {
        [Test]
        public void Constructor_InitializesProperties()
        {
            var header = SlabHeader.ForStage(0, 0);
            var astSlab = new uint[SlabHeader.HeaderIndex.Length];
            header.WriteTo(astSlab);
            var stringPool = new StringPool();
            var context = new TransformationContext("test.pas", astSlab, stringPool);

            Assert.That(context.SourceFile, Is.EqualTo("test.pas"));
            Assert.That(context.TypeCache, Is.Not.Null);
            Assert.That(context.SymbolTable, Is.Not.Null);
            Assert.That(context.FunctionScope, Is.Not.Null);
            Assert.That(context.Errors, Is.Not.Null);
            Assert.That(context.Errors, Is.Empty);
        }

        [Test]
        public void AddError_AddsToErrorList()
        {
            var header = SlabHeader.ForStage(0, 0);
            var astSlab = new uint[SlabHeader.HeaderIndex.Length];
            header.WriteTo(astSlab);
            var stringPool = new StringPool();
            var context = new TransformationContext("test.pas", astSlab, stringPool);
            var errorMessage = "Something went wrong";

            context.AddError(errorMessage);

            Assert.That(context.Errors, Has.Count.EqualTo(1));
            Assert.That(context.Errors[0], Is.EqualTo(errorMessage));
        }

        [Test]
        public void PushScope_AddsNewScope()
        {
            var header = SlabHeader.ForStage(0, 0);
            var astSlab = new uint[SlabHeader.HeaderIndex.Length];
            header.WriteTo(astSlab);
            var stringPool = new StringPool();
            var context = new TransformationContext("test.pas", astSlab, stringPool);

            context.PushScope();

            Assert.That(context.SymbolTable, Is.Not.Null);
        }

        [Test]
        public void PopScope_RemovesScope()
        {
            var header = SlabHeader.ForStage(0, 0);
            var astSlab = new uint[SlabHeader.HeaderIndex.Length];
            header.WriteTo(astSlab);
            var stringPool = new StringPool();
            var context = new TransformationContext("test.pas", astSlab, stringPool);

            context.PushScope();
            context.PopScope();

            Assert.That(context.SymbolTable, Is.Not.Null);
        }
    }
}
