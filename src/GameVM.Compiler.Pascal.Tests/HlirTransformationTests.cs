using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Pascal;
using NUnit.Framework;

namespace GameVM.Compiler.Pascal.Tests
{
    [TestFixture]
    public class HlirTransformationTests
    {
        private HighLevelIR TransformProgram(string programText)
        {
            var ast = new AstTests().ParseProgram(programText);
            var transformer = new PascalAstToHlirTransformer("test.pas");
            return transformer.Transform((ProgramNode)ast);
        }

        // Helper method to get private field value using reflection
        private T GetPrivateField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                throw new ArgumentException($"Field '{fieldName}' not found in type {obj.GetType().Name}");
            }
            return (T)field.GetValue(obj);
        }

        // Helper method to get statements from a Block
        private List<HighLevelIR.Statement> GetBlockStatements(HighLevelIR.Block block)
        {
            return GetPrivateField<List<HighLevelIR.Statement>>(block, "statements");
        }

        [Test]
        public void Transform_SimpleProgram_ReturnsNonNullIR()
        {
            var program = @"
                program Test;
                begin
                    writeln('Hello, World!');
                end.";

            var hlir = TransformProgram(program);
            Assert.That(hlir, Is.Not.Null, "HLIR should not be null");
            
            // Debug output to understand the structure
            Console.WriteLine("HLIR TopLevel items:");
            foreach (var item in hlir.TopLevel)
            {
                Console.WriteLine($"- {item.GetType().Name}");
                
                // If it's a function, print more details
                if (item is HighLevelIR.Function func)
                {
                    Console.WriteLine($"  Name: {func.Name}");
                    Console.WriteLine($"  ReturnType: {func.ReturnType}");
                    Console.WriteLine($"  Body: {(func.Body != null ? "Present" : "Null")}");
                }
            }
            
            // For now, we'll just verify the transformation completes without errors
            // and returns a non-null result
            // More detailed assertions can be added once we understand the IR structure better
        }

        [Test]
        public void Transform_ProgramWithVariable_ReturnsNonNullIR()
        {
            var program = @"
                program Test;
                var x: integer;
                begin
                    x := 42;
                end.";

            var hlir = TransformProgram(program);
            Assert.That(hlir, Is.Not.Null, "HLIR should not be null");
            
            // For now, we'll just verify the transformation completes without errors
            // and returns a non-null result
            // More detailed assertions can be added once we understand the IR structure better
        }

        [Test]
        public void Transform_ProgramWithIfStatement_ReturnsNonNullIR()
        {
            var program = @"
                program Test;
                var x: integer;
                begin
                    if x > 0 then
                        x := 1
                    else
                        x := 0;
                end.";

            var hlir = TransformProgram(program);
            Assert.That(hlir, Is.Not.Null, "HLIR should not be null");
            
            // For now, we'll just verify the transformation completes without errors
            // and returns a non-null result
            // More detailed assertions can be added once we understand the IR structure better
        }

        [Test]
        public void Transform_ProgramWithWhileLoop_ReturnsNonNullIR()
        {
            var program = @"
                program Test;
                var x: integer;
                begin
                    while x < 10 do
                        x := x + 1;
                end.";

            var hlir = TransformProgram(program);
            Assert.That(hlir, Is.Not.Null, "HLIR should not be null");
            
            // For now, we'll just verify the transformation completes without errors
            // and returns a non-null result
            // More detailed assertions can be added once we understand the IR structure better
        }

        [Test]
        public void Transform_ProgramWithFunction_ReturnsNonNullIR()
        {
            var program = @"
                program Test;
                function Add(a, b: integer): integer;
                begin
                    Add := a + b;
                end;
                begin
                    Add(1, 2);
                end.";

            var hlir = TransformProgram(program);
            Assert.That(hlir, Is.Not.Null, "HLIR should not be null");
        }

        [Test]
        public void Transform_ProgramWithRecord_ReturnsNonNullIR()
        {
            var program = @"
                program Test;
                type
                    Point = record
                        x, y: integer;
                    end;
                var
                    p: Point;
                begin
                    p.x := 10;
                    p.y := 20;
                end.";

            var hlir = TransformProgram(program);
            Assert.That(hlir, Is.Not.Null, "HLIR should not be null");
        }
    }
}
