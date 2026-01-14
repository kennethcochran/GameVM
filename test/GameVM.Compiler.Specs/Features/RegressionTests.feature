Feature: Regression Tests
  As a developer
  I want to ensure that previously fixed bugs do not reappear
  So that the compiler remains stable

  Scenario: Bug #001 - Invalid Program Heading
    Given the following Pascal program:
      """
      program Bug001
      begin
          writeln('test');
      end.
      """
    When I compile the program
    Then the compilation fails
    And the error message contains "mismatched input 'begin'"

  Scenario: Bug #002 - Register Clobbering
    Given the following Pascal program:
      """
      program Bug002;
      function Helper: Integer;
      begin
          Helper := 42;
      end;
      begin
          writeln(Helper);
      end.
      """
    When I compile the program
    Then the compilation succeeds

  Scenario: Bug #003 - Zero Page Overflow
    Given the following Pascal program:
      """
      program Bug003;
      var a, b, c, d, e, f, g, h, i, j: Integer;
      begin
          a := 1; b := 2; c := 3; d := 4; e := 5;
          f := 6; g := 7; h := 8; i := 9; j := 10;
      end.
      """
    When I compile the program
    Then the compilation succeeds

  Scenario: Bug #004 - Constant Folding
    Given the following Pascal program:
      """
      program Bug004;
      var x: Integer;
      begin
          x := 10 + 20 * 2;
      end.
      """
    When I compile the program
    Then the compilation succeeds
    # 10 + 40 = 50 ($32)
    # Depending on optimization level, it might be folded
    # If not folded, it should still be correct

  Scenario: Bug #005 - Dead Code
    Given the following Pascal program:
      """
      program Bug005;
      var x: Integer;
      begin
          x := 1;
      end.
      """
    When I compile the program
    Then the compilation succeeds

  Scenario: Bug #006 - Type Mismatch
    Given the following Pascal program:
      """
      program Bug006;
      var x: Integer;
      begin
          x := 'string';
      end.
      """
    When I compile the program
    Then the compilation fails

  Scenario: Bug #007 - Loop Optimization
    Given the following Pascal program:
      """
      program Bug007;
      var i, sum: Integer;
      begin
          sum := 0;
          for i := 1 to 10 do
              sum := sum + i;
      end.
      """
    When I compile the program
    Then the compilation succeeds

  Scenario: Bug #008 - Stack Overflow
    Given the following Pascal program:
      """
      program Bug008;
      procedure Recursive(n: Integer);
      begin
          if n > 0 then Recursive(n - 1);
      end;
      begin
          Recursive(5);
      end.
      """
    When I compile the program
    Then the compilation succeeds

  Scenario: Bug #009 - ROM Size
    Given the following Pascal program:
      """
      program Bug009;
      begin
      end.
      """
    When I compile the program
    Then the compilation succeeds
    # Verify ROM is 4KB (4096 bytes)
    And the output binary should be 4096 bytes long

  Scenario: Bug #010 - Performance
    Given the following Pascal program:
      """
      program Bug010;
      var i, x: Integer;
      begin
          for i := 1 to 100 do
              x := x + 1;
      end.
      """
    When I compile the program
    Then the compilation succeeds
