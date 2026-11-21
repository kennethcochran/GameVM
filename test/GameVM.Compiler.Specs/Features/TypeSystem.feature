Feature: Type System
  As a developer
  I want strong type checking
  So that type errors are caught at compile time

  Scenario: Valid type assignment
    Given a Pascal program with compatible types
    When I compile the program
    Then the compilation succeeds
    And type checking passes

  Scenario: Invalid type assignment
    Given a Pascal program with incompatible types
    When I compile the program
    Then the compilation fails
    And a type error is reported

  Scenario: Implicit type conversion
    Given a Pascal program with implicit type conversion
    When I compile the program
    Then the compilation succeeds
    And conversion is applied correctly

  Scenario: Array bounds checking
    Given a Pascal program with array access
    When I compile the program
    Then array bounds are validated
    And out-of-bounds access is detected

  Scenario: Named type resolves to its underlying type for type checking
    Given the following Pascal program:
      """
      program Test;
      type TScore = Integer;
      var s: TScore;
      begin
        s := 'hello';
      end.
      """
    When I compile the program
    Then the compilation fails
    And the error message contains "Type mismatch"

  Scenario: Variable declared with a named type compiles and stores its value
    Given the following Pascal program:
      """
      program Test;
      type TScore = Integer;
      var s: TScore;
      begin
        s := 42;
      end.
      """
    When I compile the program
    Then the output binary should contain the hex sequence "a9 2a 85 80"

