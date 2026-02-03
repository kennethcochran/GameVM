---
title: "Test Specifications"
description: "Test cases and validation criteria for the GameVM cross-compiler"
author: "GameVM Team"
created: "2025-09-24"
updated: "2026-01-31"
version: "1.1.0"
---

# Test Specifications

## 1. Unit Tests

### 1.1 Instruction Verification
Verification of the LLIR execution model via a reference simulator.

```csharp
[Test]
public void Test_ADD_Accumulator_Overflow() {
    var vm = new LLIR_Simulator();
    vm.A = 0xFF;
    vm.Execute(OpCodes.ADD, 0x01);
    Assert.AreEqual(0x00, vm.A);
    Assert.IsTrue(vm.Flags.Carry);
}
```

### 1.2 Dispatch Table Generation
Ensures that the compiler emits a valid jump table for the chosen dispatch technique (DTC, ITC, TTC).

## 2. Behavioral Tests (MAME)

Ensures emitted binaries behave correctly on hardware-accurate emulators.

### 2.1 Visual Regression (NTSC Golden Frames)
```yaml
# test/suites/atari_drawing.yaml
target: atari2600
rom: drawing_test.bin
expectations:
  - frame: 60
    snapshot: gold_frames/atari_draw_01.png
    tolerance: 0.05
```

### 2.2 Memory Snapshot Validation
```yaml
# test/suites/ps1_vram_load.yaml
target: psx
rom: vram_test.bin
expectations:
  - time: 2.5s
    address: 0x80100000
    expected_data: [0xDE, 0xAD, 0xBE, 0xEF]
```

## 3. Performance & Cycle Targets

### 3.1 Superinstruction Cycle Counts
Ensures that promoted intrinsics meet their timing budget.

```yaml
# test/perf/collision_super.yaml
test: CircleCollision
profile: m68k_DTC
max_cycles: 45
```

## 4. Test Environment

### 4.1 Requirements
- **MAME 0.250+**: Required for CLI-based headless execution.
- **Host Toolchain**: C# (.NET 8.0) or C++17 depending on backend.
- **Target Profiles**: Hardware-specific ROM headers and memory maps.

## 5. Test Execution

### 5.1 CLI Verification
```bash
# Execute ROM and verify memory state
gamevm-test --target atari2600 --rom test_acc.bin --expect-register A=0x42
```

## 6. Test Maintenance

### 6.1 Flaky Hardware Timing
- Tests that depend on cycle-exact hardware behavior (like "Racing the Beam") must be flagged with `@timing_sensitive`.
- Emulators must be set to deterministic mode (disable dynamic recompiler if necessary).
