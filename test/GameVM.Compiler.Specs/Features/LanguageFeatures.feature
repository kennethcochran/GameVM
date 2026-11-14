Feature: Pascal Language Features
  As a developer
  I want to use Pascal language features
  So that I can write expressive programs

  Scenario: Control flow statements
    Given a Pascal program with control flow statements
    When I compile the program
    Then the compilation succeeds
    And control flow is compiled correctly

  Scenario: Function calls
    Given a Pascal program with function calls
    When I compile the program
    Then the compilation succeeds
    And function calls are handled correctly

  Scenario: Built-in functions fail loudly (no console on the 2600)
    Given a Pascal program using write and writeln
    When I compile the program
    Then the compilation fails
    And the error message contains "not supported on the Atari 2600 target"

  Scenario: Variable scope
    Given a Pascal program with global and local variables
    When I compile the program
    Then the compilation succeeds
    And variable scope is handled correctly

  Scenario: Function declaration
    Given the following Pascal program:
      """
      program Test;
      var x: Integer;
      function Bump: Integer;
      begin
        x := 42;
        Bump := 1;
      end;
      begin
        x := 0;
        Bump;
      end.
      """
    When I compile the program
    Then the compilation succeeds
    And the output binary should contain the hex sequence "a9 2a 85 80"

