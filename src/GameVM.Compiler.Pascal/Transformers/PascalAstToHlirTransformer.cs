using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadata;
using GameVM.Compiler.Core.Utilities;

namespace GameVM.Compiler.Pascal.Transformers
{
    public class PascalAstToHlirTransformer
    {
        private readonly string _sourceFile;

        public PascalAstToHlirTransformer(string sourceFile = "<source>")
        {
            _sourceFile = sourceFile;
        }

        public HighLevelIR Transform(uint[] astSlab)
        {
            var hlir = new HighLevelIR { SourceFile = _sourceFile };
            
            if (astSlab == null || astSlab.Length < SlabHeader.HeaderIndex.Length)
            {
                hlir.Errors.Add("Invalid AST slab: too small");
                return hlir;
            }

            var header = SlabHeader.Read(astSlab);
            if (!header.HasValidMagic())
            {
                hlir.Errors.Add("Invalid AST slab: invalid magic number");
                return hlir;
            }

            // Parse the AST slab to create meaningful HLIR structures
            ParseAstSlabToHlir(astSlab, hlir);

            return hlir;
        }

        private void ParseAstSlabToHlir(uint[] astSlab, HighLevelIR hlir)
        {
            // Create a basic module structure
            var module = new HlModule
            {
                Name = "PascalProgram"
            };

            hlir.Modules.Add(module);

            // Parse instructions from the AST slab
            int offset = SlabHeader.HeaderIndex.Length;
            while (offset < astSlab.Length)
            {
                var metadata = astSlab[offset];
                var kind = MetadataDecoder.DecodeKind(metadata);
                var size = MetadataDecoder.DecodeSize(metadata);
                var argCount = MetadataDecoder.DecodeArgCount(metadata);
                
                if (size == 0)
                {
                    hlir.Errors.Add($"Block at offset {offset} has zero size (corrupt slab)");
                    break;
                }

                // Extract arguments
                var args = new List<uint>();
                for (int i = 0; i < Math.Min(argCount, size - 1); i++)
                {
                    if (offset + 1 + i < astSlab.Length)
                    {
                        args.Add(astSlab[offset + 1 + i]);
                    }
                }

                // Process based on instruction kind
                switch (kind)
                {
                    case VARIABLE_DECLARATION:
                        ProcessVariableDeclaration(args, module);
                        break;
                    case ASSIGNMENT:
                        ProcessAssignment(args, module);
                        break;
                    case LITERAL_INT:
                        // Literals are typically handled as part of expressions
                        break;
                    case IDENTIFIER:
                        // Identifiers are typically handled as part of expressions
                        break;
                    // Add more cases as needed
                }

                offset += (int)size;
            }
        }

        private void ProcessVariableDeclaration(List<uint> args, HlModule module)
        {
            // VARIABLE_DECLARATION: [metadata, typeKind, varNameHash]
            if (args.Count >= 2)
            {
                var typeKind = (byte)args[0];
                var varNameHash = args[1];
                
                // Create a variable symbol
                var variable = new HighLevelIR.Variable
                {
                    Name = $"var_{varNameHash:x8}", // Using hash as name for now
                    Type = GetPascalType(typeKind),
                    SourceFile = _sourceFile
                };
                
                // Add to module variables (avoid duplicates)
                if (!module.Variables.Any(v => v.Name == variable.Name))
                {
                    module.Variables.Add(variable);
                }
            }
        }

        private void ProcessAssignment(List<uint> args, HlModule module)
        {
            // ASSIGNMENT: [metadata, targetOffset, valueOffset]
            if (args.Count >= 2)
            {
                var targetHash = args[0];
                
                // Create a simple assignment statement
                var assignment = new HighLevelIR.Assignment
                {
                    Target = $"var_{targetHash:x8}",
                    Value = new HighLevelIR.Literal(0, new HighLevelIR.BasicType(_sourceFile, "integer") { Name = "integer" }, _sourceFile)
                };
                
                // Add to the main function
                var mainFunc = EnsureMainFunction(module);
                mainFunc.Body.Statements.Add(assignment);
            }
        }

        private HighLevelIR.Function EnsureMainFunction(HlModule module)
        {
            // Look for existing main function
            foreach (var func in module.Functions)
            {
                if (func.Name == "main")
                {
                    return func;
                }
            }
            
            // Create main function if not found
            var mainFunc = new HighLevelIR.Function
            {
                Name = "main",
                ReturnType = new HighLevelIR.BasicType(_sourceFile, "void") { Name = "void" },
                Body = new HighLevelIR.Block(_sourceFile)
            };
            
            module.Functions.Add(mainFunc);
            return mainFunc;
        }

        private HighLevelIR.HlType GetPascalType(byte typeKind)
        {
            return typeKind switch
            {
                1 => new HighLevelIR.BasicType(_sourceFile, "integer") { Name = "integer" },
                2 => new HighLevelIR.BasicType(_sourceFile, "string") { Name = "string" },
                3 => new HighLevelIR.BasicType(_sourceFile, "boolean") { Name = "boolean" },
                _ => new HighLevelIR.BasicType(_sourceFile, "unknown") { Name = "unknown" }
            };
        }
    }
}