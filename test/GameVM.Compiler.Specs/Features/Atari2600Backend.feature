Feature: Atari 2600 Backend
  As a developer
  I want the compiler to generate valid Atari 2600 machine code
  So that I can run programs on vintage hardware

  Scenario: Hello World (Register Access)
    Given the following Pascal program:
      """
      program HelloWorld;
      var
          COLUBK: integer;
      begin
          COLUBK := 10;
      end.
      """
    When I compile the program
    Then the compilation succeeds
    And the output binary should contain the hex sequence "a9 0a 85 09"

  Scenario: Constants
    Given the following Pascal program:
      """
      program Constants;
      const
          BLUE = 128;
      var
          COLUBK: integer;
      begin
          COLUBK := BLUE;
      end.
      """
    When I compile the program
    Then the compilation succeeds
    And the output binary should contain the hex sequence "a9 80 85 09"

  Scenario: Memory Layout
    Given the following Pascal program:
      """
      program MemoryLayout;
      var
          x, y: integer;
      begin
          x := 1;
          y := 2;
      end.
      """
    When I compile the program
    Then the compilation succeeds
    And the output binary should contain the hex sequence "a9 01 85 80"
    And the output binary should contain the hex sequence "a9 02 85 81"

  Scenario: Arithmetic
    Given the following Pascal program:
      """
      program Arithmetic;
      var
          x: integer;
      begin
          x := 1 + 2;
      end.
      """
    When I compile the program
    Then the compilation succeeds
    # Note: 1 + 2 = 3. Expecting LDA #$03, STA $80
    And the output binary should contain the hex sequence "a9 03 85 80"

  Scenario: Register Allocation
    Given the following Pascal program:
      """
      program RegisterAllocation;
      var x, y, z: Integer;
      begin
          x := 1;
          y := 2;
          z := x + y;
      end.
      """
    When I compile the program
    Then the compilation succeeds
    And the output binary should contain the hex sequence "a9 01 85 80"
    # z := x + y must add y's VALUE: LDA $80; CLC; ADC $81; STA $82
    # (never ADC #$81 — that would add y's address, not its value)
    And the output binary should contain the hex sequence "a5 80 18 65 81 85 82"

  Scenario: Hardware Registers (TIA)
    Given the following Pascal program:
      """
      program HardwareRegisters;
      var
          COLUBK, COLUPF, COLUP0, COLUP1: integer;
      begin
          COLUBK := 1;
          COLUPF := 2;
          COLUP0 := 3;
          COLUP1 := 4;
      end.
      """
    When I compile the program
    Then the compilation succeeds
    And the output binary should contain the hex sequence "a9 01 85 09"
    And the output binary should contain the hex sequence "a9 02 85 08"
    And the output binary should contain the hex sequence "a9 03 85 06"
    And the output binary should contain the hex sequence "a9 04 85 07"

  Scenario: ROM Generation (Size and Vectors)
    Given the following Pascal program:
      """
      program ROMGeneration;
      begin
      end.
      """
    When I compile the program
    Then the compilation succeeds
    And the output binary should be 4096 bytes long
    # Atari 2600 ROMs have vectors at the end
    # Reset vector at $FFFC (offset $0FFC in 4KB ROM)
    # The actual value depends on the entry point, but it should be present
  Scenario: Unary minus on a constant folds to two's complement
    Given the following Pascal program:
      """
      program UnaryMinus;
      var
          x: integer;
      begin
          x := -5;
      end.
      """
    When I compile the program
    Then the compilation succeeds
    And the output binary should contain the hex sequence "a9 fb 85 80"

  Scenario: Bitwise and, or, and not compute their values
    Given the following Pascal program:
      """
      program BitwiseOps;
      var x, y, z: Integer;
      begin
          y := 12;
          z := 10;
          x := y and z;
          x := y or z;
          x := not y;
      end.
      """
    When I compile the program
    Then the compilation succeeds
    # y and z (12 and 10 = 8) must use the 6502 AND: LDA $81; AND $82; STA $80
    # (never the comparison path, which would store 1)
    And the output binary should contain the hex sequence "a5 81 25 82 85 80"
    # y or z (12 or 10 = 14) must use the 6502 ORA: LDA $81; ORA $82; STA $80
    And the output binary should contain the hex sequence "a5 81 05 82 85 80"
    # not y (not 12 = 243) must complement with EOR #$FF: LDA $81; EOR #$FF; STA $80
    # (never dropped — today the whole statement vanishes silently)
    And the output binary should contain the hex sequence "a5 81 49 ff 85 80"

  Scenario: And, or, and not used as branch conditions are evaluated before branching
    Given the following Pascal program:
      """
      program CondOps;
      var a, b, x: Integer;
      begin
          a := 1;
          b := 0;
          x := 0;
          if (a or b) then x := 1;
          if (a and b) then x := 2;
          if not a then x := 3;
      end.
      """
    When I compile the program
    Then the compilation succeeds
    # a or b as a condition must be evaluated with the 6502 ORA: LDA $80; ORA $81
    # (never compared — today the condition lowers to a CMP of a against b)
    And the output binary should contain the hex sequence "a5 80 05 81"
    # a and b as a condition must be evaluated with the 6502 AND: LDA $80; AND $81
    And the output binary should contain the hex sequence "a5 80 25 81"
    # not a as a condition must complement with EOR #$FF: LDA $80; EOR #$FF
    # (never dropped — today the branch is unconditional and no complement is emitted)
    And the output binary should contain the hex sequence "a5 80 49 ff"

