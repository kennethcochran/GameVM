Feature: Pascal Language Extended
  As a developer
  I want to use various Pascal language features
  So that I can write complex programs for the Atari 2600

  Scenario: Arithmetic Operations
    Given the following Pascal program:
      """
      program Arithmetic;
      var x, y, z: Integer;
      begin
          x := 5;
          y := 3;
          z := x + y;
          z := x - y;
      end.
      """
    When I compile the program
    Then the compilation succeeds
    And the output binary should contain the hex sequence "a9 05 85 80"

  Scenario: Comparisons
    Given the following Pascal program:
      """
      program Comparisons;
      var x, y: Integer;
      var flag: Boolean;
      begin
          flag := x = y;
          flag := x <> y;
          flag := x < y;
      end.
      """
    When I compile the program
    Then the compilation succeeds

  Scenario: Nested Blocks
    Given the following Pascal program:
      """
      program NestedBlocks;
      begin
          begin
              begin
              end;
          end;
      end.
      """
    When I compile the program
    Then the compilation succeeds

  Scenario: Type Conversion
    Given the following Pascal program:
      """
      program TypeConversion;
      var x: Integer;
          y: Real;
      begin
          x := 42;
          y := x;
      end.
      """
    When I compile the program
    Then the compilation succeeds

  Scenario: Variable Scope and Function Calls
    Given the following Pascal program:
      """
      program VarScope;
      var global: Integer;
      function Add(a, b: Integer): Integer;
      begin
          Add := a + b;
      end;
      begin
          global := Add(5, 3);
      end.
      """
    When I compile the program
    Then the compilation succeeds

  Scenario: Built-in Functions
    Given the following Pascal program:
      """
      program BuiltinFunctions;
      begin
          write('Hello');
          writeln('World');
      end.
      """
    When I compile the program
    Then the compilation succeeds

  Scenario: Complex Expressions
    Given the following Pascal program:
      """
      program Expressions;
      var x, y, z: Integer;
      begin
          x := 5;
          y := 3;
          z := (x + y) * 2 - 1;
      end.
      """
    When I compile the program
    Then the compilation succeeds
    And the output binary should contain the hex sequence "a9 05 85 80"

  Scenario: Loops
    Given the following Pascal program:
      """
      program Loops;
      var i: Integer;
      begin
          i := 0;
          while i < 10 do
          begin
              i := i + 1;
          end;
      end.
      """
    When I compile the program
    Then the compilation succeeds

  Scenario: Edge Cases - Zero Value
    Given the following Pascal program:
      """
      program EdgeCases;
      var x: Integer;
      begin
          x := 0;
          if x = 0 then
              x := 1;
      end.
      """
    When I compile the program
    Then the compilation succeeds
    And the output binary should contain the hex sequence "a9 00 85 80"
