# Code Generation

## Overview

GameVM supports multiple code generation strategies, letting developers choose the right trade-offs for their game.

## Virtual Machine Approaches

### Token Threaded Code (TTC)
- Most compact bytecode representation
- Smallest VM implementation
- Higher runtime overhead
- Ideal for: Games needing maximum ROM space for assets

### Indirect Threaded Code (ITC)
- Better performance than TTC
- Small code size increase over TTC
- Uses jump table for dispatch
- Ideal for: Balance of ROM space and moderate performance

### Direct Threaded Code (DTC)
- Excellent performance on simple CPUs
- Moderate code size
- Direct jump to implementation
- Ideal for: Performance-critical code on retro hardware

### Subroutine Threaded Code (STC)
- Fast on modern CPUs with branch prediction
- Performance varies on simple CPUs
- Larger code size but better instruction cache usage
- Ideal for: Code that will run on both retro and modern systems

### Native Code Generation
- Direct machine code output
- No runtime overhead
- Platform-specific code
- Ideal for: Timing-critical routines

### Mixed-Mode Execution
- Combine different strategies within the same game
- Use native code for timing-critical sections
- Use threaded code for general game logic
- Balance between size and speed per module

## Dynamic VM Generation

GameVM analyzes your code at build time to create a specialized VM:

### Analysis Phase
- Identifies actually used instructions
- Discovers common code patterns
- Maps data access patterns
- Analyzes timing requirements

### Optimization Phase
- Creates custom opcodes for common patterns
- Optimizes memory layout for your game
- Generates specialized dispatch code
- Implements platform-specific features

### Resource Analysis
- Detailed size analysis for each strategy
- Memory usage estimates
- Execution speed comparisons
- Custom opcode efficiency metrics
