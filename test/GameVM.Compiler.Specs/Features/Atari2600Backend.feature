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
