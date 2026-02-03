# Capability Implementation Blueprint

## 1. Overview
This document defines the technical implementation requirements for enforcing Capability Profiles across the GameVM toolchain. It ensures that the compiler, Standard Library, and HAL share a unified "Capability Awareness" during the compilation pipeline.

## 2. Compiler Implementation (`GameVM.Compiler.Core`)

### 2.1 Capability Meta-Tagging
All IR nodes and library functions must be tagged with their required extension code.
- **`MLInstruction` Metadata**: Add a `RequiredExtension` property to the `MLInstruction` base class in `MidLevelIR.cs`.
- **`LLIR` Metadata**: Similarly, update the `LowLevelIR` instructions if backend-specific checks are needed late-stage.
- **Attributes**: Use C# attributes in the Standard Library implementation (mapped via the Pascal/C# Frontends) to declare requirements.

### 2.2 The Capability Verifier (`GameVM.Compiler.Application`)
Integrate a new `ICapabilityVerifier` service into the `CompileUseCase.Execute` pipeline. 
1. **Selection**: Read the desired profile/extension string from the upgraded `CompilationOptions` (passed from `gamevm.yaml`).
2. **Timing**: The check occurs after `ConvertToMidLevelIR` and before the `Transform` to `LowLevelIR`.
3. **Action**: 
   - Walk the `MidLevelIR` graph.
   - Match `MLInstruction.RequiredExtension` against the allowed project set.
   - If a mismatch is found, query the `IEmulationLibrary` for a fallback.
   - If no fallback exists, throw a `HardwareCapabilityException`.

### 2.3 Handle the 'Z' (Vendor) Extension
- When the `Z` extension is detected (e.g., `Zatari`), the `CompileUseCase` allows the use of **Intrinsic Instructions** that are otherwise restricted.
- The `Atari2600CodeGenerator` can then emit optimized native opcodes (e.g., specific TIA strobes) that are only valid for that target.

## 3. HAL Implementation (`GameVM.Compiler.Core/Interfaces`)

### 3.1 Backend Capability Self-Reporting
Each backend code generator reports its own capabilities through the interface. This provides accurate, dynamic capability information rather than static categorization.

```csharp
// Defined in GameVM.Compiler.Core
public interface ICodeGenerator {
    byte[] Generate(LowLevelIR ir, CodeGenOptions options);
    CapabilityProfile GetCapabilityProfile();
    IEnumerable<string> GetSupportedExtensions();
}

public class CapabilityProfile {
    public CapabilityLevel BaseLevel { get; set; }
    public HashSet<string> Extensions { get; set; } = new();
    public Dictionary<string, CapabilityLevel> InjectedCapabilities { get; set; } = new();
}
```

### 3.2 Backend Implementation Examples
Every backend implements capability self-reporting:
- `Atari2600CodeGenerator`: Reports `BaseLevel = L1`, optional DPC extension
- `GenesisCodeGenerator`: Reports `BaseLevel = L4`, standard extensions
- `SNESCodeGenerator`: Reports `BaseLevel = L5`, Mode 7 capabilities

### 3.3 Interface-Based Abstraction
Refactor the HAL from a flat structure into a set of **Capability Interfaces**.

```csharp
// Defined in GameVM.Compiler.Core
public interface IHardwareExtension { string Code { get; } }

public interface ITileExtension : IHardwareExtension {
    void SetTile(int x, int y, int id);
}
```

## 4. Standard Library & Emulation (`src/GameVM.StdLib`)

### 4.1 Modular Assembly
The Standard Library source code should be organized by extension. The `LanguageFrontend` will selectively include units based on the active `CapabilityString`.

### 4.2 Emulation Library (Fallbacks)
Found in a new `GameVM.Compiler.Emulation` project. It contains generic `MidLevelIR` implementations of "Premium" extensions:
- **`SoftwareMath` (M)**: Long division/multiplication routines for 8-bit targets.
- **`SoftwareWavetable` (W)**: Software-mixing logic for targets with only Digital (DAC) output.

## 5. Deployment Pipeline
1. **Frontends**: Scan source for extension-specific calls (e.g., `Draw3D`) or **Inline LLIR** blocks (`asm { ... }`).
2. **Analysis**: `CompileUseCase` validates usage against the project's profile.
3. **Backend Transition**: `Atari2600/MidToLowLevelTransformer.cs` handles the lowering of both the compiler-generated LLIR and the developer-written Inline LLIR.
4. **Code Generation**: Final machine code emitted via `M6502Emitter.cs` or equivalent.
