# Spec: Indirect Threaded Code (ITC)

**Status:** [aspirational] — not implemented.

## Summary

ITC is a dispatch strategy that offers better performance than Token Threaded Code (TTC) at the cost of slightly larger bytecode and a more complex VM.

## Key Characteristics

- Uses a jump table for dispatch (indirect jump through a table of native addresses).
- Better performance than TTC by removing the explicit decode per instruction.
- Small code size increase over TTC (each instruction is 2 bytes: an opcode offset plus inline data).
- Ideal for: Balancing ROM space and moderate performance.

## Comparison to Implemented Strategies

- **TTC** (implemented): Compact, but dispatch overhead per instruction.
- **DTC** (implemented): Fast, but larger code size.
- **ITC** (this spec): Between the two — the middle ground.

## Out of Scope

- Native code generation.
- Mixed-mode execution.

## Notes

The `ITC` dispatch strategy is **not** currently in the `DispatchStrategy` enum. This spec exists to capture the intended design should ITC be revisited.