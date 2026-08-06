# Changed Files in slab-soa-refactor

The following files have been modified or created since work started on `slab-soa-refactor`:

## Specifications & Design
- `openspec/changes/slab-soa-refactor/design.md`
- `openspec/changes/slab-soa-refactor/proposal.md`
- `openspec/changes/slab-soa-refactor/tasks.md`

## Compiler Pipeline (Core)
- `src/GameVM.Compiler.Core/IR/Soa/InstList.cs` (New SoA InstList layout)
- `src/GameVM.Compiler.Core/IR/Soa/InstListBuilder.cs` (Builder for InstList)
- `src/GameVM.Compiler.Core/IR/Soa/InstFlags.cs`
- `src/GameVM.Compiler.Core/IR/Soa/Handles.cs`
- `src/GameVM.Compiler.Core/IR/Enums/AstNodeKind.cs`
- `src/GameVM.Compiler.Core/IR/Interfaces/IIRTransformer.cs`
- `src/GameVM.Compiler.Core/IR/Interfaces/ICodeGenerator.cs`
- `src/GameVM.Compiler.Core/Interfaces/ILanguageFrontend.cs`
- `src/GameVM.Compiler.Core/Interfaces/ISemanticAnalyzer.cs`
- `src/GameVM.Compiler.Core/SemanticAnalysis/BasicSemanticAnalyzer.cs`
- `src/GameVM.Compiler.Core/IR/Transformers/AstSlabToHlirSlabTransformer.cs`
- `src/GameVM.Compiler.Core/IR/Transformers/HlirSlabToMlirSlabTransformer.cs`

## Optimizers & Backend (Atari 2600 & Low/Mid level)
- `src/GameVM.Compiler.Optimizers.LowLevel/DefaultLowLevelOptimizer.cs`
- `src/GameVM.Compiler.Optimizers.MidLevel/DefaultMidLevelOptimizer.cs`
- `src/GameVM.Compiler.Backend.Atari2600/Atari2600CodeGenerator.cs`
- `src/GameVM.Compiler.Backend.Atari2600/MidToLowLevelTransformer.cs`

## Application & Services
- `src/GameVM.Compiler.Application/CompileUseCase.cs`
- `src/GameVM.Compiler.Application/Services/ILowLevelOptimizer.cs`
- `src/GameVM.Compiler.Application/Services/IMidLevelOptimizer.cs`

## C# & Pascal Frontends
- `src/GameVM.Compiler.CSharp/CSharpFrontend.cs`
- `src/GameVM.Compiler.CSharp/Transformers/CSharpToSlabVisitor.cs`
- `src/GameVM.Compiler.Pascal/PascalFrontend.cs`
- `src/GameVM.Compiler.Pascal/Transformers/PascalToSlabVisitor.cs`
- (Deleted many obsolete OOP node classes in `src/GameVM.Compiler.Pascal/` - migrated to DOD visitor/slab)

## Tests & Verification
- `test/GameVM.Compiler.Application.Tests/CompileUseCaseCapabilityTests.cs`
- `test/GameVM.Compiler.Backend.Atari2600.Tests/Atari2600CodeGeneratorTests.cs`
- `test/GameVM.Compiler.Backend.Atari2600.Tests/Optimizers/LowLevelOptimizerTests.cs`
- `test/GameVM.Compiler.Core.Tests/BasicSemanticAnalyzerExtendedTests.cs`
- `test/GameVM.Compiler.Core.Tests/BasicSemanticAnalyzerTests.cs`
- `test/GameVM.Compiler.Core.Tests/IR/Slab/InstListTests.cs`
- `test/GameVM.Compiler.Core.Tests/IR/Transformers/AstSlabToHlirSlabTransformerTests.cs`
- `test/GameVM.Compiler.Core.Tests/IR/Transformers/HlirSlabToMlirSlabTransformerTests.cs`
- `test/GameVM.Compiler.Optimizers.LowLevel.Tests/LowLevelOptimizerTests.cs`
- `test/GameVM.Compiler.Pascal.Tests/Transformers/PascalToSlabVisitorTests.cs`
- `test/GameVM.Compiler.Specs/DebugPipelineTests.cs`
