# Internal Assembly API: Template Interpreter Design

## 1. Overview
GameVM utilizes an internal C# DSL (Domain Specific Language) to define architecture-specific machine code patterns. This API allows the compiler to generate target-native instructions directly from C# code at compile-time, eliminating the need for external assemblers or separate assembly files for each backend.

## 2. Design Philosophy: The "Template Interpreter"
Inspired by the **HotSpot Template Interpreter (JVM)**, this approach avoids the use of slow, C-switch-based dispatch loops. Instead:
- **Direct Emission**: The compiler uses these APIs to emit the "Inner Loop" and "Instruction Handlers" as optimized machine code blocks tailored to the specific game.
- **Native Implementation**: Writing the interpreter logic in C# (via this API) ensures that the emission logic is version-controlled and tightly integrated with the compiler's IR (Intermediate Representation) mapping.
- **Micro-Optimization**: By working at the C# level, the compiler can dynamically choose the most efficient addressing modes (e.g., Zero-Page on 6502) for each instruction handler based on whole-program analysis.

## 3. API Structure

### 3.1 Architecture-Specific Modules
Each target architecture (6502, Z80, MIPS, SH-2) implements its own static API class. These classes provide methods that correspond directly to physical opcodes.

```csharp
// Example: Emitting a DTC 'NEXT' loop for 6502
public static class Assembly6502 
{
    public static void LDX_Indirect(byte zeroPagePtr) { /* Emits 0xA1 ... */ }
    public static void JMP_Indirect(ushort address) { /* Emits 0x6C ... */ }
    
    // Higher-level 'Template' composition
    public static void EmitNextLoop() {
        // Defines the standardized 'NEXT' logic for this platform
        LDA_Indirect(Registers.PC);
        INC_ZeroPage(Registers.PC);
        STA_ZeroPage(Registers.DispatchPtr);
        JMP_Indirect(Registers.DispatchPtr);
    }
}
```

### 3.2 The Abstraction Layer (Future Goal)
The long-term vision is a **High-Level Interpreter Definition** (HLID) that allows the developer to define the VM logic (e.g., "Add the value at R0 to A") in an IR-neutral way. The compiler will then map this definition to the architecture-specific Assembly APIs.

- **Current Step**: Manual definition of interpreters in C# using arch-specific opcodes.
- **Future Step**: A generic "Interpreter Specification" that lowers automatically to 6502, Z80, or MIPS templates.

## 4. Key Benefits
1. **Performance**: Emitted code is direct, non-switched, and leverages hardware-specific idioms (e.g., 68000 auto-increment).
2. **No External Dependencies**: No need to maintain or bundle target-specific cross-assemblers.
3. **Consistency**: The same C# toolchain that parses Python or Pascal also generates the final hardware-native entry point.
4. **Tooling Integration**: C# unit tests can verify the bytecode-to-native mapping for every instruction in isolation.

## 5. Implementation Status
- **Phase 1**: Instruction emission for 6502 and Z80 (Basic DTC).
- **Phase 2 (Active)**: Expansion to MIPS (PS1/N64) and SH-2 (Saturn).
- **Phase 3**: Introduction of the Cross-Architecture Interpreter Abstraction.
