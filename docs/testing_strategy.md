# GameVM Compiler: Long-term Testing Strategy

To ensure a production-grade compiler infrastructure, the following testing tiers should be implemented beyond the existing Unit, BDD, and Lit tests.

## 1. Execution Verification (MAME Integration)
**Status: Priority Next Step**
Verifying the *behavior* of the generated code by running it in a cycle-accurate emulator.
- **Tool**: MAME (Multi-Purpose Emulation Framework).
- **Goal**: Verify hardware state (RAM, TIA registers) after executing a test program.
- **Workflow**: 
  1. Compile Pascal to `.bin`.
  2. Launch MAME in a headless/automated state.
  3. Execute for $N$ cycles and dump memory/registers.
  4. Compare state against expected results.

## 2. Diagnostic & Negative Testing
Ensuring the compiler fail gracefully and provide helpful feedback.
- **Goal**: Verify that invalid code leads to specific error codes/messages rather than crashes.
- **Approach**: Maintain a suite of `.pas` files with intentional errors (type mismatches, scope violations, etc.) and use `lit` to verify the error output.

## 3. Property-Based Testing & Fuzzing
Discovering edge cases by generating large volumes of varied input.
- **Goal**: Find internal compiler crashes or logic errors in complex transformations.
- **Approach**: Use a grammar-based fuzzer to generate valid Pascal programs and verify that the compiler can process them without internal errors (ICEs).

## 4. Differential Testing
Ensuring that optimizations do not change the program's observable behavior.
- **Goal**: Validate the correctness of the optimizer.
- **Approach**: Run the same program through the pipeline with different optimization levels (`-O0` vs `-O3`) and verify that the end-state in the emulator is identical.

## 5. Regression Testing
Preventing bugs from re-emerging.
- **Approach**: Every bug fixed should be accompanied by a specific `lit` or integration test that would have caught it.
